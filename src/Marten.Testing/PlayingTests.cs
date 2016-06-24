﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Baseline;
using Marten.Linq;
using Marten.Schema;
using Marten.Testing.Documents;
using Marten.Testing.Events.Projections;
using Marten.Testing.Fixtures;
using Marten.Util;
using StoryTeller;
using StoryTeller.Engine;
using StructureMap;
using Xunit;
using Xunit.Abstractions;

namespace Marten.Testing
{
    public class PlayingTests
    {
        private readonly ITestOutputHelper _output;

        public PlayingTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public void run_st_spec()
        {
            using (var runner = new SpecRunner<NulloSystem>())
            {
                var results = runner.Run("Event Store/Event Capture/Version a stream as part of event capture");


                runner.OpenResultsInBrowser();
            }
        }

        [Fact]
        public void can_generate_a_computed_index()
        {
            using (var store = TestingDocumentStore.Basic())
            {
                store.Schema.EnsureStorageExists(typeof(User));

                var mapping = store.Schema.MappingFor(typeof(User));
                var sql = mapping.As<DocumentMapping>().FieldFor(nameof(User.UserName)).As<JsonLocatorField>().ToComputedIndex(mapping.Table)
                    .Replace("d.data", "data");

                using (var conn = store.Advanced.OpenConnection())
                {
                    conn.Execute(cmd => cmd.Sql(sql).ExecuteNonQuery());
                }

                using (var session = store.OpenSession())
                {
                    var query =
                        session.Query<User>()
                            .Where(x => x.UserName == "hank")
                            .ToCommand(FetchType.FetchMany)
                            .CommandText;

                    _output.WriteLine(query);

                    var plan = session.Query<User>().Where(x => x.UserName == "hank").Explain();
                    _output.WriteLine(plan.ToString());
                }
            }
        }

        [Fact]
        public void fetch_index_definitions()
        {
            using (var store = TestingDocumentStore.For(_ =>
            {
                _.Schema.For<User>().Searchable(x => x.UserName);
            }))
            {
                store.BulkInsert(new User[] {new User {UserName = "foo"}, new User { UserName = "bar" }, });
            }
        }

        [Fact]
        public void try_some_linq_queries()
        {
            using (var container = Container.For<DevelopmentModeRegistry>())
            {
                var store = container.GetInstance<IDocumentStore>();
                store.BulkInsert(Target.GenerateRandomData(200).ToArray());

                using (var session = store.LightweightSession())
                {
                    Debug.WriteLine(session.Query<Target>().Where(x => x.Double > 1.0).Count());
                    Debug.WriteLine(session.Query<Target>().Where(x => x.Double > 1.0 && x.Double < 33).Count());
                }
            }
        }

        public void try_out_select()
        {
            using (var container = Container.For<DevelopmentModeRegistry>())
            {
                var store = container.GetInstance<IDocumentStore>();
                store.Advanced.Clean.CompletelyRemoveAll();
                store.BulkInsert(Target.GenerateRandomData(200).ToArray());

                using (var session = store.QuerySession())
                {
                    session.Query<Target>().Select(x => x.Double).ToArray();
                }

            }
        }
    }


}