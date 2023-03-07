using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IBackgroundJobService
    {
        string Enqueue(Expression<Func<Task>> methodCall);

        string Schedule(Expression<Func<Task>> methodCall, DateTimeOffset enqueueAt);

        bool Delete(string jobId);
    }
}