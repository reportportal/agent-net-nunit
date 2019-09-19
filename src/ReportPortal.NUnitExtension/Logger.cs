using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportPortal.NUnitExtension
{
    public class Tracer
    {
        private const string VariableName = "ReportPortalTraceLevel";
        private static IEnumerable<string> _availableSourceLevels = Enum.GetNames(typeof(SourceLevels));

        private static TraceSource _instance;

        private static readonly object SyncRoot = new object();

        private static readonly string TraceLevel = Environment.GetEnvironmentVariable(VariableName);
        private static readonly bool LoggerEnabled = _availableSourceLevels.Contains(Environment.GetEnvironmentVariable(VariableName));

        private Tracer()
        {
        }

        public static TraceSource Instance
        {
            get
            {
                lock (SyncRoot)
                {
                    if (_instance == null)
                    {
                        _instance = new TraceSource("TraceSource");
                        if (LoggerEnabled)
                        {
                            Init();
                        }
                    }

                    return _instance;
                }
            }
        }

        private static void Init()
        {
            Trace.AutoFlush = true;

            _instance.Switch = new SourceSwitch("sourceSwitch", TraceLevel);
            _instance.Listeners.Clear();
            _instance.Listeners.Add(new DefaultTraceListener());
            DefaultTraceListener dtl = (DefaultTraceListener)_instance.Listeners["Default"];
            dtl.LogFileName = "Trace.log";
        }
    }
}