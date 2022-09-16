use std::env;

pub extern fn print_args() -> ()
{
    for str in env::args() {
        println!("'{str}'");
    }
}

#[cfg(test)]
mod tests
{
    use wasmtime::{Engine, Module, Linker, Store, TrapCode};

    const WAT: &str = r#"(module
        (export "run" (func $run))
        (export "run_div_zero" (func $run_div_zero))
        (export "run_div_zero_with_result" (func $run_div_zero_with_result))
        (export "run_stack_overflow" (func $run_stack_overflow))
      
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
      
        (func $run_div_zero_with_result (result i32)
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
      )"#;

    #[test]
    fn stackoverflow()
    {
        let engine = Engine::default();
        let module = Module::new(&engine, WAT).expect("Module::new");
        let linker = Linker::new(&engine);
        let mut store = Store::new(&engine, 4);
        let instance = linker.instantiate(&mut store, &module).unwrap();
        let run = instance.get_typed_func::<(), (), _>(&mut store, "run_stack_overflow").unwrap();

        let result = run.call(&mut store, ());

        assert!(result.is_err());
        assert_eq!(result.err().unwrap().trap_code().unwrap(), TrapCode::StackOverflow);
    }

    #[test]
    fn divzero()
    {
        let engine = Engine::default();
        let module = Module::new(&engine, WAT).expect("Module::new");
        let linker = Linker::new(&engine);
        let mut store = Store::new(&engine, 4);
        let instance = linker.instantiate(&mut store, &module).unwrap();
        let run = instance.get_typed_func::<(), (), _>(&mut store, "run_div_zero").unwrap();

        let result = run.call(&mut store, ());

        assert!(result.is_err());
        assert_eq!(result.err().unwrap().trap_code().unwrap(), TrapCode::IntegerDivisionByZero);
    }
}