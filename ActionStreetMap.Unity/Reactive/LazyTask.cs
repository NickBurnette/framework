﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ActionStreetMap.Infrastructure.Reactive
{
    /// <summary />
    public abstract class LazyTask
    {
        /// <summary />
        public enum TaskStatus
        {
            /// <summary />
            WaitingToRun,
            /// <summary />
            Running,
            /// <summary />
            Completed,
            /// <summary />
            Canceled,
            /// <summary />
            Faulted
        }
        /// <summary />
        public TaskStatus Status { get; protected set; }

        protected readonly BooleanDisposable cancellation = new BooleanDisposable();
        /// <summary />
        public abstract Coroutine Start();
        /// <summary />
        public void Cancel()
        {
            if (Status == TaskStatus.WaitingToRun || Status == TaskStatus.Running)
            {
                Status = TaskStatus.Canceled;
                cancellation.Dispose();
            }
        }
        /// <summary />
        public static LazyTask<T> FromResult<T>(T value)
        {
            return LazyTask<T>.FromResult(value);
        }

        /// <summary />
        public static Coroutine WhenAll(params LazyTask[] tasks)
        {
            return WhenAll(tasks.AsEnumerable());
        }
        /// <summary />
        public static Coroutine WhenAll(IEnumerable<LazyTask> tasks)
        {
            var coroutines = tasks.Select(x => x.Start()).ToArray();

            return MainThreadDispatcher.StartCoroutine(WhenAllCore(coroutines));
        }

        static IEnumerator WhenAllCore(Coroutine[] coroutines)
        {
            foreach (var item in coroutines)
            {
                // wait sequential, but all coroutine is already started, it's parallel
                yield return item;
            }
        }
    }
    /// <summary />
    public class LazyTask<T> : LazyTask
    {
        readonly IObservable<T> source;

        T result;
        /// <summary />
        public T Result
        {
            get
            {
                if (Status != TaskStatus.Completed) throw new InvalidOperationException("Task is not completed");
                return result;
            }
        }

        /// <summary>
        /// If faulted stock error. If completed or canceld, returns null.
        /// </summary>
        public Exception Exception { get; private set; }
        /// <summary />
        public LazyTask(IObservable<T> source)
        {
            this.source = source;
            this.Status = TaskStatus.WaitingToRun;
        }

        public override Coroutine Start()
        {
            if (Status != TaskStatus.WaitingToRun) throw new InvalidOperationException("Task already started");

            Status = TaskStatus.Running;

            var coroutine = source.StartAsCoroutine(
                onResult: x => { result = x; Status = TaskStatus.Completed; },
                onError: ex => { Exception = ex; Status = TaskStatus.Faulted; },
                cancel: new CancellationToken(cancellation));

            return coroutine;
        }

        public override string ToString()
        {
            switch (Status)
            {
                case TaskStatus.WaitingToRun:
                    return "Status:WaitingToRun";
                case TaskStatus.Running:
                    return "Status:Running";
                case TaskStatus.Completed:
                    return "Status:Completed, Result:" + Result.ToString();
                case TaskStatus.Canceled:
                    return "Status:Canceled";
                case TaskStatus.Faulted:
                    return "Status:Faulted, Result:" + Result.ToString();
                default:
                    return "";
            }
        }
        /// <summary />
        public static LazyTask<T> FromResult(T value)
        {
            var t = new LazyTask<T>(null);
            t.result = value; ;
            t.Status = TaskStatus.Completed;
            return t;
        }
    }
    /// <summary />
    public static class LazyTaskExtensions
    {
        /// <summary />
        public static LazyTask<T> ToLazyTask<T>(this IObservable<T> source)
        {
            return new LazyTask<T>(source);
        }
    }
}