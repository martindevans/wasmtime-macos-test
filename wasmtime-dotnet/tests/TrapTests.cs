using System;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class TrapFixture : ModuleFixture
    {
        protected override string ModuleFileName => "Trap.wat";
    }

    public class TrapTests : IClassFixture<TrapFixture>, IDisposable
    {
        private TrapFixture Fixture { get; set; }

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        public TrapTests(TrapFixture fixture)
        {
            Fixture = fixture;
            Store = new Store(Fixture.Engine);
            Linker = new Linker(Fixture.Engine);
        }

        [Fact]
        public void ItIncludesAStackTrace()
        {
            Action action = () =>
            {
                var instance = Linker.Instantiate(Store, Fixture.Module);
                var run = instance.GetAction("run");
                run.Should().NotBeNull();
                run();
            };

            action
                .Should()
                .Throw<TrapException>()
                .Where(e => e.Frames.Count == 4 &&
                            e.Frames[0].FunctionName == "third" &&
                            e.Frames[1].FunctionName == "second" &&
                            e.Frames[2].FunctionName == "first" &&
                            e.Frames[3].FunctionName == "run")
                .WithMessage("wasm trap: wasm `unreachable` instruction executed*");
        }

        [Fact]
        public void ItCatchesAStackOverflow()
        {
            Action action = () =>
            {
                var instance = Linker.Instantiate(Store, Fixture.Module);
                var run = instance.GetAction("run_stack_overflow");
                run.Should().NotBeNull();
                run();
            };

            action
                .Should()
                .Throw<TrapException>();
        }

        [Fact]
        public void CopiedFromProgram()
        {
            const string WAT = @"(module
                (export ""run""(func $run))
                (export ""run_div_zero""(func $run_div_zero))
                (export ""run_div_zero_with_result""(func $run_div_zero_with_result))
                (export ""run_stack_overflow""(func $run_stack_overflow))

                (func $run
                    (call $first)
                )
                (func $first
                    (call $second)
                )
                (func $second
                    (call $third)
                )
                (func $third
                    unreachable
                )

                (func $run_div_zero_with_result(result i32)
                    (i32.const 1)
                    (i32.const 0)
                    (i32.div_s)
                )
                
                (func $run_div_zero
                    (call $run_div_zero_with_result)
                    (drop)
                )
                
                (func $run_stack_overflow
                    (call $run_stack_overflow)
                )
            )";


            var config =  new Config().WithReferenceTypes(true);
            var engine = new Engine(config);
            var module = Module.FromText(engine, "name", WAT);
            var linker = new Linker(engine);
            var store = new Store(engine);
            var instance = linker.Instantiate(store, module);
            var run = instance.GetAction("run_stack_overflow")!;

            try
            {
                run();
                throw new NotImplementedException("No trap :(");
            }
            catch (TrapException ex)
            {
                Console.WriteLine("TRAPPED OK. FRAMES: " + (ex.Frames?.Count ?? 0));
            }
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
