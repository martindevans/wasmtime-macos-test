﻿using Wasmtime;

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



var engine = new Engine();
var module = Module.FromText(engine, "name", WAT);
var linker = new Linker(engine);
var store = new Store(engine);
var instance = linker.Instantiate(store, module);
var run = instance.GetAction("run_stack_overflow");

try
{
    run();
    throw new NotImplementedException("No trap :(");
}
catch (TrapException ex)
{
    Console.WriteLine("TRAPPED OK. FRAMES: " + (ex.Frames?.Count ?? 0));
}
