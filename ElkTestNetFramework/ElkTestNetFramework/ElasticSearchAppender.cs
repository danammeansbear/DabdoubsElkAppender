﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Core;
using ElkTestNetFramework.Models;
using Uri = System.Uri;
using log4net.ElasticSearch;

namespace ElkTestNetFramework
{
    public class ElasticSearchAppender : BufferingAppenderSkeleton
    {
        static readonly string AppenderType = typeof(ElasticSearchAppender).Name;

        const int DefaultOnCloseTimeout = 30000;
        readonly ManualResetEvent workQueueEmptyEvent;

        IRepository repository;
        int queuedCallbackCount;
        List<FieldNameOverride> fieldNameOverrides = new List<FieldNameOverride>();
        List<FieldValueReplica> fieldValueReplicas = new List<FieldValueReplica>();

        public ElasticSearchAppender()
        {
            workQueueEmptyEvent = new ManualResetEvent(true);
            OnCloseTimeout = DefaultOnCloseTimeout;
        }

        public string ConnectionString { get; set; }

        public int OnCloseTimeout { get; set; }

        public string RollingIndexNameDateFormat { get; set; } = "yyyy.MM.dd";

        public string IndexTypeName { get; set; } = "exs_log4net_";

        public override void ActivateOptions()
        {
            base.ActivateOptions();

            ServicePointManager.Expect100Continue = false;

            // Custom certificate validation callback (for development purposes)
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            try
            {
                Validate(ConnectionString);
            }
            catch (Exception ex)
            {
                HandleError("Failed to validate ConnectionString in ActivateOptions", ex);
                return;
            }

            ConnectionString += string.Format(";BufferSize={0}", BufferSize);
            repository = CreateRepository(ConnectionString);
        }


        public void AddFieldNameOverride(FieldNameOverride fieldNameOverride)
        {
            fieldNameOverrides.Add(fieldNameOverride);
        }

        public void AddFieldValueReplica(FieldValueReplica fieldValueReplica)
        {
            fieldValueReplicas.Add(fieldValueReplica);
        }

        protected override void SendBuffer(LoggingEvent[] events)
        {
            BeginAsyncSend();
            if (TryAsyncSend(events)) return;
            EndAsyncSend();
            HandleError("Failed to async send logging events in SendBuffer");
        }
       /// <summary>
       /// this method is called when the appender is closed
       /// </summary>
        protected override void OnClose()
        {
            base.OnClose();

            if (TryWaitAsyncSendFinish()) return;
            HandleError("Failed to send all queued events in OnClose");
        }

        protected virtual IRepository CreateRepository(string connectionString)
        {
            ElkTestNetFramework.Models.Uri.Init(RollingIndexNameDateFormat);

            var overrides = fieldNameOverrides.ToDictionary(x => x.Original, x => x.Replacement);
            var resolver = new CustomDataContractResolver
            {
                FieldNameChanges = overrides,
                FieldValueReplica = fieldValueReplicas,
            };
            return Repository.Create(connectionString, resolver);
        }

        protected virtual bool TryAsyncSend(IEnumerable<LoggingEvent> events)
        {
            return ThreadPool.QueueUserWorkItem(SendBufferCallback, logEvent.CreateMany(events));
        }

        protected virtual bool TryWaitAsyncSendFinish()
        {
            return workQueueEmptyEvent.WaitOne(OnCloseTimeout, false);
        }

        private void BeginAsyncSend()
        {
            workQueueEmptyEvent.Reset();
            Interlocked.Increment(ref queuedCallbackCount);
        }

        private async void SendBufferCallback(object state)
        {
            try
            {
                await repository.AddAsync((IEnumerable<logEvent>)state, BufferSize);
            }
            catch (Exception ex)
            {
                HandleError("Failed to add logEvents to {0} in SendBufferCallback".With(repository.GetType().Name), ex);
            }
            finally
            {
                EndAsyncSend();
            }
        }

        private void EndAsyncSend()
        {
            if (Interlocked.Decrement(ref queuedCallbackCount) > 0)
                return;
            workQueueEmptyEvent.Set();
        }

        void HandleError(string message)
        {
            ErrorHandler.Error("{0} [{1}]: {2}.".With(AppenderType, Name, message));
        }

        void HandleError(string message, Exception ex)
        {
            ErrorHandler.Error("{0} [{1}]: {2}.".With(AppenderType, Name, message), ex, ErrorCode.GenericFailure);
        }

        static void Validate(string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException("connectionString");
            }

            if (connectionString.Length == 0)
            {
                throw new ArgumentException("connectionString is empty", "connectionString");
            }
        }
    }

    public class FieldNameOverride : IOptionHandler
    {
        public string Original { get; set; }

        public string Replacement { get; set; }

        public void ActivateOptions()
        {
        }
    }

    public class FieldValueReplica : IOptionHandler
    {
        public string Original { get; set; }

        public string Replica { get; set; }

        public void ActivateOptions()
        {
        }
    }
}
