﻿using HealthMonitor.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HealthMonitor
{
    public class SelfHostInitializer : IDisposable
    {
        IList<IDisposable> _selfHostedTerminals = new List<IDisposable>();
        ManualResetEventSlim _waitHandle = new ManualResetEventSlim(false);
        Process _hubProcess;

        public void Initialize(string connectionString)
        {
            var selfHostedTerminals = GetSelfHostedTerminals();
            try
            {
                foreach (SelfHostedTerminalsElement terminal in selfHostedTerminals)
                {
                    Type calledType = Type.GetType(terminal.Type);
                    if (calledType == null)
                    {
                        throw new ArgumentException(
                            string.Format(
                                "Unable to instantiate the terminal type {0}.",
                                terminal.Type
                            )
                        );
                    }

                    if (terminal.Type.IndexOf("HubWeb") > -1)
                    {
                        // Run the Hub in a separate appdomain to avoid conflict with StructureMap configurations for
                        // termianls and the Hub.
                        StartHub(terminal, connectionString);
                    }
                    else {
                        MethodInfo curMethodInfo = calledType.GetMethod("CreateServer", BindingFlags.Static | BindingFlags.Public);
                        _selfHostedTerminals.Add((IDisposable)curMethodInfo.Invoke(null, new string[] { terminal.Url }));
                    }
                }
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Determine directory where Hub Launcher utility is located. 
        /// It depends on the current build configuration.
        /// </summary>
        /// <returns></returns>
        private string GetHubLauncherDirectory()
        {
            const string CONFIG =
#if DEV
            "dev";
#elif RELEASE
            "release";
#else
            "debug";
#endif
            var directory = Directory.GetParent(Directory.GetParent(Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName).FullName);
            return Path.Combine(directory.FullName, "HealthMonitor.HubLauncher\\bin\\", CONFIG);
        }

        /// <summary>
        /// The function spins off a new process to lanuch the Hub. This is necessary in order 
        /// to avoid component configuration for the Hub and terminals mixing up. 
        /// </summary>
        /// <param name="hub"></param>
        /// <param name="connectionString"></param>
        private void StartHub(SelfHostedTerminalsElement hub, string connectionString)
        {
            Console.WriteLine("Starting HubLauncher...");
            string hubLauncherDirectory = GetHubLauncherDirectory();
            string args = "--endpoint " + hub.Url + " --selfHostFactory \"" + hub.Type + "\" --connectionString \"" + connectionString + "\"";
            ProcessStartInfo psi = new ProcessStartInfo(Path.Combine(hubLauncherDirectory, "HealthMonitor.HubLauncher.exe"), args);
            Console.WriteLine("HubLauncher Path: " + psi.FileName);
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.CreateNoWindow = true;

            _hubProcess = new Process();
            _hubProcess.StartInfo = psi;
            _hubProcess.OutputDataReceived += _hubProcess_OutputDataReceived;
            _hubProcess.ErrorDataReceived += _hubProcess_OutputDataReceived;
            _hubProcess.EnableRaisingEvents = true;
            bool started = _hubProcess.Start();
            _hubProcess.BeginOutputReadLine();
            _hubProcess.BeginErrorReadLine();

            if (!started)
            {
                throw new Exception("Cannot start HubLauncher for an unknown reason. Test runner aborted.");
            }

            // Wait for the message from HubLauncher indicating that the Hub has been launched. 
            _waitHandle.Wait(new TimeSpan(0, 3, 0));
            Console.WriteLine("Proceeding to Tests");

        }

        private void _hubProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;

            // Hub Launcher posts  "Listening..." to the standard output when the Hub is ready. 
            if (e.Data.IndexOf("Listening...") > -1)
            {
                // HubLauncher is ready, can start tests
                _waitHandle.Set();
            }
            Console.WriteLine("HubLauncher:\\> " + e.Data);
        }

        private SelfHostedTerminalsCollection GetSelfHostedTerminals()
        {
            var healthMonitorCS = (HealthMonitorConfigurationSection)
                ConfigurationManager.GetSection("healthMonitor");

            if (healthMonitorCS == null || healthMonitorCS.SelfHostedTerminals == null)
            {
                return null;
            }

            return healthMonitorCS.SelfHostedTerminals;
        }

        public void Dispose()
        {
            foreach (IDisposable selfHostedTerminal in _selfHostedTerminals)
            {
                selfHostedTerminal.Dispose();
            }

            if (_hubProcess != null && !_hubProcess.HasExited)
            {
                Console.WriteLine("Terminating HubLauncher...");
                _hubProcess.StandardInput.WriteLine("quit");
                _hubProcess.WaitForExit();
            }
        }
    }
}
