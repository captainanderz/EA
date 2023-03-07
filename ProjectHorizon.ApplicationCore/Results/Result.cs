using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Results
{
    public abstract class Result
    {
        public bool Success { get; protected set; }
        public bool Failure => !Success;
    }

    public abstract class Result<T> : Result
    {
        private T? _data;

        protected Result(T? data)
        {
            _data = data;
        }

        public T? Data
        {
            get => Success ? _data : throw new Exception($"You can't access .{nameof(Data)} when .{nameof(Success)} is false");
            set => _data = value;
        }
    }
}
