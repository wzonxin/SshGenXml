using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace SshGenXml
{
    class Program
    {
        private const string _defUser = "wuzhongxin";
        private const string _defPassword = "zonxin123";
        private const string _defServerFolder = "server";
        private static bool _downloadFile = false;

        static void Main(string[] args)
        {
            string inputUser = string.Empty;
            string inputPassword = string.Empty;
            string inputServerFolder = string.Empty;

            if (args.Length >= 1)
                inputUser = args[0];
            if (args.Length >= 2)
                inputPassword = args[1];
            if (args.Length >= 3)
                inputServerFolder = args[2];

            string username = string.IsNullOrEmpty(inputUser) ? _defUser : inputUser;
            string password = string.IsNullOrEmpty(inputUser) ? _defPassword : inputPassword;
            string serverFolder = string.IsNullOrEmpty(inputServerFolder) ? _defServerFolder : inputServerFolder;

            Console.WriteLine("using user:" + username);
            //Console.WriteLine("using password:" + password);

            const string host = "192.168.1.2";
            Console.WriteLine("Connect SSH:" + host);
            SshClient sshClient = new SshClient(host, username, password);
            try
            {
                sshClient.Connect();
            }
            catch (SshAuthenticationException e)
            {
                Console.WriteLine("svn 用户名密码不正确");
                //throw;
                return;
            }

            //Console.WriteLine("Execute command:build_for_client.sh");
            string executeCmd = $"cd {serverFolder} && ./build_for_client.sh";
            Console.WriteLine($"Execute command:{executeCmd}");

            using var command = sshClient.CreateCommand(executeCmd);
            string executeResult = command.Execute();
            //WriteFile(executeResult);
            //Console.Write(executeResult);

            bool genOk = executeResult.Contains("export csharp files sucess");
            if (!genOk)
            {
                Console.WriteLine("export csharp has some error. check it");
                return;
            }

            Console.WriteLine("Copy remote dr file");

            SftpClient sftpClient = new SftpClient(host, username, password);
            sftpClient.Connect();

            using Stream svrFileStream = File.Create("./ResMeta.dr");
            using Stream clientFileStream = File.Create("./ResMeta_cli.dr");
            sftpClient.DownloadFile($"/home/users/{username}/{serverFolder}/resource/dr/ResMeta.dr", svrFileStream, OnDownloadFinish);
            sftpClient.DownloadFile($"/home/users/{username}/{serverFolder}/resource/dr/ResMeta_cli.dr", clientFileStream, OnDownloadFinish);
            svrFileStream.Close();
            clientFileStream.Close();

            Console.WriteLine("Copy dr finish");

            if (_downloadFile)
            {
                Console.WriteLine("Copy remote csharp file");
                var ignoreList = IgnoreFileList();
                string remoteCSharpDir = $"/home/users/{username}/{serverFolder}/resource/csharp";
                var files = sftpClient.ListDirectory(remoteCSharpDir);//SFTP folder from where the file is to be download
                foreach (var file in files)
                {
                    string remoteFileName = file.Name;

                    if (ignoreList.Contains(remoteFileName))
                    {
                        Console.WriteLine("ignore remote file :" + remoteFileName);
                    }

                    if (!Directory.Exists("CSharp"))
                        Directory.CreateDirectory("CSharp");

                    if (remoteFileName == ".." || remoteFileName == ".")
                        continue;

                    using Stream file1 = File.Create(($"./csharp/{remoteFileName}"));
                    sftpClient.DownloadFile($"{remoteCSharpDir}/{remoteFileName}", file1);
                    file1.Flush();
                    file1.Close();
                }

                Console.WriteLine("Copy CSharp file finish");
            }
        }

        private static List<string> IgnoreFileList()
        {
            const string excludeTxt = "./ExcludeProto.txt";
            string[] fileAllText;
            if (File.Exists(excludeTxt))
            {
                fileAllText = File.ReadAllLines(excludeTxt);
            }
            else
            {
                fileAllText = new string[0];
            }

            return new List<string>(fileAllText);
        }

        private static void OnDownloadFinish(ulong value)
        {
        }

        private static void WriteFile(string fileContent)
        {
            var file = File.Create("./result.txt");
            file.Write(Encoding.UTF8.GetBytes(fileContent));
            file.Flush();
            file.Close();
        }
    }
}
