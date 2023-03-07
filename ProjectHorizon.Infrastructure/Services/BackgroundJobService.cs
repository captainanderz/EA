using Hangfire;
using Hangfire.Server;
using ProjectHorizon.ApplicationCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ProjectHorizon.Infrastructure.Services
{
    /// <summary>
    /// <p>This is an abstraction over the static Hangfire classes. This is useful for decoupling and testing.</p>
    /// <p>Feel free to add any method you need from <see cref="Hangfire.BackgroundJob"/>.</p>
    /// </summary>
    public class BackgroundJobService : IBackgroundJobService
    {
        private const int numberOfJobs = 200;

        public string Enqueue(Expression<Func<Task>> methodCall)
        {
            if (IsJobAlreadyInProcessing(methodCall))
            {
                return string.Empty;
            }

            if (IsJobAlreadyScheduled(methodCall))
            {
                return string.Empty;
            }

            return BackgroundJob.Enqueue(methodCall);
        }

        public string Schedule(Expression<Func<Task>> methodCall, DateTimeOffset enqueueAt)
        {
            if (IsJobAlreadyInProcessing(methodCall))
            {
                return string.Empty;
            }

            if (IsJobAlreadyScheduled(methodCall))
            {
                return string.Empty;
            }

            return BackgroundJob.Schedule(methodCall, enqueueAt);
        }

        private Tuple<string, KeyValuePair<Type, object>[]> GetMethodInfo<T>(Expression<Func<T>> expression)
        {
            MethodCallExpression? body = (MethodCallExpression)expression.Body;
            string methodName = body.Method.Name;
            List<KeyValuePair<Type, object>>? parameters = new List<KeyValuePair<Type, object>>();

            foreach (Expression? argument in body.Arguments)
            {
                if (argument.Type == typeof(PerformContext))
                {
                    break;
                }

                MemberExpression? exp = ResolveMemberExpression(argument);
                Type? type = argument.Type;

                object? value = GetArgumentValue(exp);

                parameters.Add(new KeyValuePair<Type, object>(type, value));
            }

            return new Tuple<string, KeyValuePair<Type, object>[]>(methodName, parameters.ToArray());
        }

        private object GetArgumentValue(Expression element)
        {
            if (element is ConstantExpression)
            {
                return (element as ConstantExpression).Value;
            }

            LambdaExpression? lambda = Expression.Lambda(Expression.Convert(element, element.Type));
            return lambda.Compile().DynamicInvoke();
        }

        private MemberExpression ResolveMemberExpression(Expression expression)
        {
            if (expression is MemberExpression)
            {
                return (MemberExpression)expression;
            }
            else
            {
                if (expression is UnaryExpression expression1)
                {
                    // if casting is involved, Expression is not x => x.FieldName but x => Convert(x.Fieldname)
                    return (MemberExpression)expression1.Operand;
                }
                else
                {
                    throw new NotSupportedException(expression.ToString());
                }
            }
        }

        private bool IsJobAlreadyInProcessing(Expression<Func<Task>> methodCall)
        {
            Tuple<string, KeyValuePair<Type, object>[]>? methodInfo = GetMethodInfo(methodCall);

            Hangfire.Storage.IMonitoringApi? jobMonitor = JobStorage.Current.GetMonitoringApi();
            int paramCount = methodInfo.Item2.Length;

            bool result = false;
            jobMonitor.ProcessingJobs(0, numberOfJobs)
                .Where(j => j.Value.Job.Method.Name == methodInfo.Item1 &&
                paramCount == j.Value.Job.Args.Where(p => p != null).Count())
                .ToList()
                .ForEach(job =>
                {
                    int countDown = paramCount;
                    for (int i = 0; i < paramCount; i++)
                    {
                        object? param1 = job.Value.Job.Args[i];
                        object? param2 = methodInfo.Item2[i].Value;

                        if (param1.GetType() == param2.GetType() && param1.Equals(param2))
                        {
                            countDown--;
                        }

                        if (countDown == 0)
                        {
                            result = true;
                            break;
                        }

                    }
                });

            return result;
        }

        private bool IsJobAlreadyScheduled(Expression<Func<Task>> methodCall)
        {
            Tuple<string, KeyValuePair<Type, object>[]>? methodInfo = GetMethodInfo(methodCall);

            Hangfire.Storage.IMonitoringApi? jobMonitor = JobStorage.Current.GetMonitoringApi();
            int paramCount = methodInfo.Item2.Length;

            bool result = false;

            jobMonitor.ScheduledJobs(0, numberOfJobs)
                .Where(j => j.Value.Job is not null)
                .Where(j =>
                {
                    string? jobMehodName = j.Value.Job.Method.Name;
                    string? methodName = methodInfo.Item1;

                    int jobParamCount = j.Value.Job.Args.Where(p => p != null).Count();

                    return jobMehodName == methodName && jobParamCount == paramCount;
                })
                .ToList()
                .ForEach(job =>
                {
                    int countDown = paramCount;
                    for (int i = 0; i < paramCount; i++)
                    {
                        object? param1 = job.Value.Job.Args[i];
                        object? param2 = methodInfo.Item2[i].Value;

                        if (param1.GetType() == param2.GetType() && param1.Equals(param2))
                        {
                            countDown--;
                        }

                        if (countDown == 0)
                        {
                            result = true;
                            break;
                        }
                    }
                });

            return result;
        }

        public bool Delete(string jobId)
        {
            return BackgroundJob.Delete(jobId);
        }
    }
}