using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using LINQPad;
using LINQPad.Extensibility.DataContext;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace MongoDB.LINQPadDriver
{
    public sealed class MongoDriver : DynamicDataContextDriver
    {
        static MongoDriver()
        {
            // Debugger.Launch();
            // Uncomment the following code to attach to Visual Studio's debugger when an exception is thrown.
#if DEBUG
            AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
            {
                if (args.Exception.StackTrace.Contains(typeof(MongoDriver).Namespace))
                {
                    Debugger.Launch();
                }
            };
#endif
        }

        public override string Name => "MongoDB Driver " + Version;
        public override string Author => "mkjeff";
        public override Version Version => typeof(MongoDriver).Assembly.GetName().Version;

        public override bool AreRepositoriesEquivalent(IConnectionInfo c1, IConnectionInfo c2)
            => c1.DatabaseInfo.CustomCxString == c2.DatabaseInfo.CustomCxString
            && c1.DatabaseInfo.Database == c2.DatabaseInfo.Database;

        public override IEnumerable<string> GetAssembliesToAdd(IConnectionInfo cxInfo)
        {
            var customAssemblyPath = cxInfo.CustomTypeInfo.GetAbsoluteCustomAssemblyPath();
            return customAssemblyPath == null
                ? (IEnumerable<string>)(new[] { "*" })
                : (IEnumerable<string>)(new[] { "*", cxInfo.CustomTypeInfo.GetAbsoluteCustomAssemblyPath() });
        }

        public override IEnumerable<string> GetNamespacesToAdd(IConnectionInfo cxInfo)
        {
            return new[]
            {
                "MongoDB.Driver",
                "MongoDB.Driver.Linq",
            }.Concat(cxInfo.DatabaseInfo.Server.Split(';'));
        }

        private static readonly HashSet<string> ExcludedCommand = new HashSet<string>
        {
            "isMaster",
            "buildInfo",
            "saslStart",
            "saslContinue",
            "getLastError",
        };

        public override void InitializeContext(IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager)
        {
            //Debugger.Launch();
            var mongoClientSettings = MongoClientSettings.FromUrl(new MongoUrl(cxInfo.DatabaseInfo.CustomCxString));
            mongoClientSettings.ClusterConfigurator = cb => cb
                .Subscribe<CommandStartedEvent>(e =>
                {
                    if (!ExcludedCommand.Contains(e.CommandName))
                    {
                        executionManager.SqlTranslationWriter.WriteLine(e.Command.ToJson(new JsonWriterSettings { Indent = true }));
                    }
                })
                .Subscribe<CommandSucceededEvent>(e =>
                {
                    if (!ExcludedCommand.Contains(e.CommandName))
                    {
                        executionManager.SqlTranslationWriter.WriteLine($"\t Duration = {e.Duration} \n");
                    }
                });

            var client = new MongoClient(mongoClientSettings);
            var mongoDatabase = client.GetDatabase(cxInfo.DatabaseInfo.Database);

            context.GetType().GetMethod("Initial", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(context, new[] { mongoDatabase });
        }

        public override string GetConnectionDescription(IConnectionInfo cxInfo)
            => "MongoDb - " + cxInfo.DatabaseInfo.CustomCxString + " properties";

        public override bool ShowConnectionDialog(IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions)
            => new ConnectionDialog(cxInfo).ShowDialog() == true;

        private string GetClass(string table) => string.Concat(table[0].ToString().ToUpper(), table.AsSpan(1));

        public override List<ExplorerItem> GetSchemaAndBuildAssembly(
            IConnectionInfo cxInfo, AssemblyName assemblyToBuild, ref string nameSpace, ref string typeName)
        {

            if ((bool?)cxInfo.DriverData.Element("Debug") ?? false)
            {
                Debugger.Launch();
            }
            var @namespaces = cxInfo.DatabaseInfo.Server.Split(';');
            var customAssemblyPath = cxInfo.CustomTypeInfo.GetAbsoluteCustomAssemblyPath();
            var types = new HashSet<string>();
            if (customAssemblyPath != null)
            {
                types = LoadAssemblySafely(customAssemblyPath).GetTypes()
                    .Where(a => @namespaces.Contains(a.Namespace) && a.IsPublic)
                    .Select(a => a.Name)
                    .ToHashSet();
            }

            var mongoClientSettings = MongoClientSettings.FromUrl(new MongoUrl(cxInfo.DatabaseInfo.CustomCxString));
            var client = new MongoClient(mongoClientSettings);
            var collections =
                (from c in client.GetDatabase(cxInfo.DatabaseInfo.Database).ListCollectionNames().ToList()
                //  where char.IsUpper(c[0]) // ignore system collection
                 orderby c
                 select (collectionName: c , className : GetClass(c), type: types.Contains(c) ? c : nameof(BsonDocument))
                 ).ToList();

            var source = @$"using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;


{ string.Join(Environment.NewLine, @namespaces.Select(n => "using " + n + ";")) }

namespace {nameSpace}" +
@"
{
   // The main typed data class. The user's queries subclass this, so they have easy access to all its members.
   public class " + typeName + @"
   {
        public IMongoDatabase GetDatabase() => _db;
        private IMongoDatabase _db;
        internal void Initial(IMongoDatabase db)
        {
            _db = db;
        }
    
        private Lazy<IMongoCollection<T>> InitCollection<T>(string collectionName)
            => new Lazy<IMongoCollection<T>>(()=>_db.GetCollection<T>(collectionName));
        
        public " + typeName + @"()
        {
" + string.Join("\n", collections.Select(c =>
             $"            _{c.className} = InitCollection<{c.type}>(\"{c.collectionName}\");"))
+ @"
        }

" + string.Join("\n",
    collections.Select(c =>
    $@"
        private readonly Lazy<IMongoCollection<{c.type}>> _{c.className};
        public IMongoCollection<{c.type}> {c.className}_Collection() => _{c.className}.Value;
        public IQueryable<{c.type}> {c.className} => _{c.className}.Value.AsQueryable(); "))

+ @"
   }	
}"

;
            if ((bool?)cxInfo.DriverData.Element("Debug") ?? false)
            {
                NotepadHelper.ShowMessage(source, "Source for TypedDataContext");
            }

            Compile(cxInfo, source, assemblyToBuild.CodeBase,
                ((customAssemblyPath == null) ? new string[0] :
                    Directory.GetFiles(new FileInfo(customAssemblyPath).DirectoryName, "*.dll"))
                .Concat(new[]{
                    typeof(IMongoDatabase).Assembly.Location,
                    typeof(BsonDocument).Assembly.Location,
                    typeof(MongoDB.Driver.Core.Configuration.ConnectionSettings).Assembly.Location
                    }));

            // We need to tell LINQPad what to display in the TreeView on the left (Schema Explorer):
            var schemas = collections.Select(a =>
                new ExplorerItem(a.className , ExplorerItemKind.QueryableObject, ExplorerIcon.Table)
                {
                    IsEnumerable = true,
                    DragText = a.className,
                });

            return schemas.ToList();
        }

        private static void Compile(IConnectionInfo cxInfo, string cSharpSourceCode, string outputFile, IEnumerable<string> customTypeAssemblyPath)
        {
            // GetCoreFxReferenceAssemblies is helper method that returns the full set of .NET Core reference assemblies.
            // (There are more than 100 of them.)
            var assembliesToReference = GetCoreFxReferenceAssemblies(cxInfo).Concat(customTypeAssemblyPath).ToArray();

            // CompileSource is a static helper method to compile C# source code using LINQPad's built-in Roslyn libraries.
            // If you prefer, you can add a NuGet reference to the Roslyn libraries and use them directly.
            var compileResult = CompileSource(new CompilationInput
            {
                FilePathsToReference = assembliesToReference,
                OutputPath = outputFile,
                SourceCode = new[] { cSharpSourceCode }
            });

            if (compileResult.Errors.Length > 0)
            {
                throw new Exception("Cannot compile typed context: " + compileResult.Errors[0]);
            }
        }

        public override ICustomMemberProvider GetCustomDisplayMemberProvider(object objectToWrite)
        {
            if (objectToWrite is BsonDocument bd)
            {
                return new BsonDocumentCustomMemberProvider(bd);
            }
            return null;
        }
        
    }


}
