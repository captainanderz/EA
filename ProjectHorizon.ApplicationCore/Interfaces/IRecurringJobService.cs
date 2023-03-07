using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ProjectHorizon.ApplicationCore.Interfaces
{
    public interface IRecurringJobService
    {
        void RegisterAll();
        void RegisterJob(string jobId, Expression<Func<Task>> job, string cronExpression);
        void RemoveJob(string jobId);
    }
}