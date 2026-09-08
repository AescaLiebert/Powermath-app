using System;
using System.Collections.Generic;
using NUnit.Framework;
using PowerMath.Diagnostics;
using UnityEngine;
using UnityEngine.TestTools;

namespace PowerMath.Tests.EditMode
{
    public sealed class AppLogTests
    {
        private sealed class TestSink : ILogSink
        {
            public List<LogMessage> Received { get; } = new();
            public void Emit(in LogMessage message) => Received.Add(message);
        }

        [SetUp]
        public void SetUp()
        {
            AppLog.ResetToDefaults();
            AppLog.ClearSinks();
        }

        [TearDown]
        public void TearDown()
        {
            AppLog.ResetToDefaults();
        }

        [Test]
        public void MinimumLevelFilteringSuppressesLowerSeverities()
        {
            var sink = new TestSink();
            AppLog.AddSink(sink);
            AppLog.MinimumLevel = LogLevel.Warning;

            AppLog.Verbose("Test", "Verbose message");
            AppLog.Debug("Test", "Debug message");
            AppLog.Info("Test", "Info message");

            Assert.AreEqual(0, sink.Received.Count);

            AppLog.Warning("Test", "Warning message");
            AppLog.Error("Test", "Error message");
            AppLog.Fatal("Test", "Fatal message");

            Assert.AreEqual(3, sink.Received.Count);
            Assert.AreEqual(LogLevel.Warning, sink.Received[0].Level);
            Assert.AreEqual(LogLevel.Error, sink.Received[1].Level);
            Assert.AreEqual(LogLevel.Fatal, sink.Received[2].Level);
        }

        [Test]
        public void LogMessageCapturesCategoryAndText()
        {
            var sink = new TestSink();
            AppLog.AddSink(sink);

            AppLog.Info("Localization", "Language set to TH");

            Assert.AreEqual(1, sink.Received.Count);
            Assert.AreEqual("Localization", sink.Received[0].Category);
            Assert.AreEqual("Language set to TH", sink.Received[0].Text);
            Assert.AreEqual(LogLevel.Info, sink.Received[0].Level);
        }

        [Test]
        public void MessageLoggedEventDispatchesToSubscribers()
        {
            LogMessage received = default;
            bool fired = false;
            Action<LogMessage> handler = msg =>
            {
                received = msg;
                fired = true;
            };

            AppLog.MessageLogged += handler;
            AppLog.Error("Auth", "Failed login attempt");
            AppLog.MessageLogged -= handler;

            Assert.IsTrue(fired);
            Assert.AreEqual("Auth", received.Category);
            Assert.AreEqual("Failed login attempt", received.Text);
            Assert.AreEqual(LogLevel.Error, received.Level);
        }

        [Test]
        public void ToStringRendersStructuredFormat()
        {
            var msg = new LogMessage(LogLevel.Info, "Combat", "Enemy defeated");
            string output = msg.ToString();

            StringAssert.Contains("[INFO]", output);
            StringAssert.Contains("[Combat]", output);
            StringAssert.Contains("Enemy defeated", output);
            StringAssert.Contains("[F:", output);
        }

        [Test]
        public void ExceptionLoggingCapturesExceptionDetails()
        {
            var sink = new TestSink();
            AppLog.AddSink(sink);

            var ex = new InvalidOperationException("Connection lost");
            AppLog.Exception("Network", ex, "Operation failed");

            Assert.AreEqual(1, sink.Received.Count);
            Assert.AreEqual(LogLevel.Error, sink.Received[0].Level);
            Assert.AreEqual("Network", sink.Received[0].Category);
            StringAssert.Contains("Operation failed", sink.Received[0].Text);
            Assert.AreSame(ex, sink.Received[0].Exception);

            string str = sink.Received[0].ToString();
            StringAssert.Contains("InvalidOperationException", str);
            StringAssert.Contains("Connection lost", str);
        }

        [Test]
        public void HistoryStoresAndCapsEntries()
        {
            AppLog.ClearHistory();

            for (int i = 0; i < 150; i++)
            {
                AppLog.Info("Loop", $"Message {i}");
            }

            var recent = AppLog.GetRecentLogs();
            Assert.AreEqual(128, recent.Count);
            Assert.AreEqual("Message 149", recent[recent.Count - 1].Text);
        }

        [Test]
        public void UnityConsoleSinkEmitsFormattedMessages()
        {
            var sink = new UnityConsoleSink();
            var warningMsg = new LogMessage(LogLevel.Warning, "TestCat", "Warning event");
            var ex = new InvalidOperationException("Sink test exception");
            var errorMsg = new LogMessage(LogLevel.Error, "TestCat", "Error with exception", ex);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*Warning event.*"));
            sink.Emit(in warningMsg);

            LogAssert.Expect(LogType.Exception, "InvalidOperationException: Sink test exception");
            sink.Emit(in errorMsg);
        }
    }
}
