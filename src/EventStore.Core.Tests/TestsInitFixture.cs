﻿using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using EventStore.Common.Utils;
using EventStore.Core.Tests.Helpers;
using NUnit.Framework;

namespace EventStore.Core.Tests
{
    [SetUpFixture]
    public class TestsInitFixture
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 1000;
            Console.WriteLine("Initializing tests (setting console loggers)...");
            SetUpDebugListeners();
            //LogManager.SetLogFactory(x => new ConsoleLogger());
            var logFactory = new LoggerFactory();
#pragma warning disable CS0618 // 类型或成员已过时
            logFactory.AddNLog();
#pragma warning restore CS0618 // 类型或成员已过时
            TraceLogger.Initialize(logFactory);

            Application.AddDefines(new[] { Application.AdditionalCommitChecks });
            LogEnvironmentInfo();

            if (!Debugger.IsAttached) { PortsHelper.InitPorts(IPAddress.Loopback); }
        }

        private void SetUpDebugListeners()
        {
#if DESKTOPCLR
            Debug.Listeners.Clear(); //prevent message box popup when assertions fail
            Debug.Listeners.Add(new ThrowExceptionTraceListener()); //all failed assertions should throw an exception to halt the tests
#endif
        }

        private void LogEnvironmentInfo()
        {
            var log = TraceLogger.GetLogger<TestsInitFixture>();

            log.LogInformation("\n{0,-25} {1} ({2}/{3}, {4})\n"
                     + "{5,-25} {6} ({7})\n"
                     + "{8,-25} {9} ({10}-bit)\n"
                     + "{11,-25} {12}\n\n",
                     "ES VERSION:", VersionInfo.Version, VersionInfo.Branch, VersionInfo.Hashtag, VersionInfo.Timestamp,
                     "OS:", OS.OsFlavor, Environment.OSVersion,
                     "RUNTIME:", OS.GetRuntimeVersion(), Marshal.SizeOf(typeof(IntPtr)) * 8,
                     "GC:", GC.MaxGeneration == 0 ? "NON-GENERATION (PROBABLY BOEHM)" : string.Format("{0} GENERATIONS", GC.MaxGeneration + 1));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            var runCount = Math.Max(1, MiniNode.RunCount);
            var msg = string.Format("Total running time of MiniNode: {0} (mean {1})\n" +
                                    "Total starting time of MiniNode: {2} (mean {3})\n" +
                                    "Total stopping time of MiniNode: {4} (mean {5})\n" +
                                    "Total run count: {6}",
                                    MiniNode.RunningTime.Elapsed, TimeSpan.FromTicks(MiniNode.RunningTime.Elapsed.Ticks / runCount),
                                    MiniNode.StartingTime.Elapsed, TimeSpan.FromTicks(MiniNode.StartingTime.Elapsed.Ticks / runCount),
                                    MiniNode.StoppingTime.Elapsed, TimeSpan.FromTicks(MiniNode.StoppingTime.Elapsed.Ticks / runCount),
                                    MiniNode.RunCount);

            Console.WriteLine(msg);
            //LogManager.Finish();
        }
    }

    internal class ThrowExceptionTraceListener : TraceListener
    {
        public ThrowExceptionTraceListener()
        {
        }

        public override void Fail(string message)
        {
            Assert.Fail(message);
        }
        public override void Fail(string message, string detailMessage)
        {
            var msg = message + detailMessage != null ? " " + detailMessage : "";
            Assert.Fail(msg);
        }

        public override void Write(string message)
        {
        }

        public override void WriteLine(string message)
        {
        }
    }
}
