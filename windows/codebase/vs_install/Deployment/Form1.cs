﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using NLog;
using Deployment.items;
using Renci.SshNet;
using File = System.IO.File;

namespace Deployment
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        private readonly string[] _args = Environment.GetCommandLineArgs();
        private readonly Dictionary<string, string> _arguments = new Dictionary<string, string>();
        private readonly Deploy _deploy;
        public readonly Dictionary<string, KurjunFileInfo> PrerequisiteFilesInfo = new Dictionary<string, KurjunFileInfo>();

        private readonly string _cloneName = $"subutai-{DateTime.Now.ToString("yyyyMMddhhmm")}";
        private readonly PrivateKeyFile[] _privateKeys = new PrivateKeyFile[] { };

        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        private void ParseArguments()
        {
            foreach (var splitted in _args.Select(argument => argument.Split(new[] { "=" }, StringSplitOptions.None)).Where(splitted => splitted.Length == 2))
            {
                _arguments[splitted[0]] = splitted[1];
                //logger.Info("Parsing arguments {0}.", _arguments["repo_tgt"]);
            }
        }

        public Form1()
        {
            InitializeComponent();
            ParseArguments();
            _deploy = new Deploy(_arguments);

            timer1.Start();
        }
        public void StageReporter(string stageName, string subStageName)
        {
            Program.form1.Invoke((MethodInvoker)delegate
            {
                if (stageName != "")
                {
                    this.labelControl1.Text = stageName;
                }
                if (subStageName != "")
                {
                    this.progressPanel1.Description = subStageName;
                }
            });
        }

        private void progressPanel1_Click(object sender, EventArgs e)
        {

        }

        #region TASKS FACTORY

        private void TaskFactory(object sender, AsyncCompletedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {

            })

               .ContinueWith((prevTask) =>
               {
                   if (_arguments["network-installation"].ToLower() == "true")
                   {
                       check_md5();

                       //unzip_repo();
                       //MessageBox.Show("Unzip repo");
                   }
               })

               .ContinueWith((prevTask) =>
               {
                   if (_arguments["network-installation"].ToLower() == "true")
                   {
                       unzip_extracted();
                       //MessageBox.Show("Unzip extracted");
                   }
               })

               .ContinueWith((prevTask) =>
               {
                   if (_arguments["params"].Contains("deploy-redist"))
                   {
                       deploy_redist();
                       //MessageBox.Show("Deploy redist

                   }
               })

               .ContinueWith((prevTask) =>
               {
                   if (_arguments["params"].Contains("prepare-vbox"))
                   {
                       prepare_vbox();
                       //MessageBox.Show("Prepare VBox");
                   }
               })
               
               .ContinueWith((prevTask) =>
               {
                   if (_arguments["params"].Contains("prepare-rh"))
                   {
                       prepare_rh();
                       //MessageBox.Show("Prepare RH");
                   }
               })

               .ContinueWith((prevTask) =>
               {
                   if (_arguments["params"].Contains("deploy-p2p"))
                   {
                       deploy_p2p();
                       //MessageBox.Show("Deploy P2P");
                   }
               })


               .ContinueWith((prevTask) =>
               {
                   //wait_mh("https:////localhost:9999/rest/v1/peer/mhpresent");
                   //wait_mh("https:////localhost:9999/rest/v1/peer/inited");
               })

               .ContinueWith((prevTask) =>
               {
                   Program.form1.Invoke((MethodInvoker) delegate
                   {
                       Program.form1.Visible = false;
                   });

                   Program.form2.Invoke((MethodInvoker) delegate
                   {
                       Program.form2.Show();
                   });
               }).ContinueWith((prevTask) =>
               {
                   Deploy.LaunchCommandLineApp($"{_arguments["appDir"]}/bin/tray/SubutaiTray.exe", "");
               });
        }
        #endregion

        #region TASK FACTORY COMPONENTS
        private void download_repo()
        {
            //DOWNLOAD REPO
            StageReporter("Downloading prerequisites", "");

            Deploy.HideMarquee();
            download_description_file("repo_descriptor");
            logger.Info("Downloading repo_descriptor");
        }

        private void download_description_file(String arg_name)
        {
            //StageReporter("", "Getting description file");

            _deploy.DownloadFile(
                url: _arguments["kurjunUrl"], 
                destination: $"{_arguments["appDir"]}/{_arguments[arg_name]}", 
                onComplete: download_prerequisites, 
                report: "Getting repo descriptor",
                async: true, 
                kurjun: true);
            
        }


        private void download_description_file_(String file_name)
        {
            //StageReporter("", "Getting description file");

            _deploy.DownloadFile(
                url: "https://cdn.subut.ai:8443/kurjun/rest/file/get?id=7a005c7441b27b8bee7425f6c275bc79",
                destination: "C:\\Subutai\\repotgt",
                onComplete: null,
                report: "Getting target descriptor",
                async: true,
                kurjun: true);

        }

        private int _prerequisitesDownloaded = 0;

        private void download_prerequisites(object sender, AsyncCompletedEventArgs e)
        {
            var rows = File.ReadAllLines($"{_arguments["appDir"]}/{_arguments["repo_descriptor"]}");

            var row = rows[_prerequisitesDownloaded];
            var folderFile = row.Split(new[] {"|"}, StringSplitOptions.None);

            var folder = folderFile[0].Trim();
            var file = folderFile[1].Trim();
            logger.Info("Downloading prerequisites: {0}.", $"{_arguments["appDir"]}/{folder}/{file}");

            if (_prerequisitesDownloaded < rows.Length - 3) //.snap? (_prerequisitesDownloaded != rows.Length - 3) 
            {
                _deploy.DownloadFile(
                    url: _arguments["kurjunUrl"],
                    destination: $"{_arguments["appDir"]}/{folder}/{file}",
                    onComplete: download_prerequisites,
                    report: $"Getting {file}",
                    async: true,
                    kurjun: true
                    );
                _prerequisitesDownloaded++;
            }
            else //if (_prerequisitesDownloaded == rows.Length - 3) //snap
            {
                var destfile = file;
                if ((_arguments["params"].Contains("dev")) || (_arguments["params"].Contains("master")))
                {
                    if (_arguments["params"].Contains("dev"))
                    {
                        _prerequisitesDownloaded++;
                    } else //master
                    {
                        _prerequisitesDownloaded += 2;
                    }
                    row = rows[_prerequisitesDownloaded];
                    folderFile = row.Split(new[] { "|" }, StringSplitOptions.None);
                    folder = folderFile[0].Trim();
                    file = folderFile[1].Trim();
                }
                //MessageBox.Show("file:" + folder + "\\" + file + "destfile:" + destfile);
                 _deploy.DownloadFile(
                    url: _arguments["kurjunUrl"],
                    destination: $"{_arguments["appDir"]}/{folder}/{file}",
                    onComplete: TaskFactory,
                    report: $"Getting {file}",
                    async: true,
                    kurjun: true);

            } 
       }

        private void check_md5()
        {
            //UNZIP REPO
            StageReporter("Verifying MD5", "");

            Deploy.HideMarquee();

            foreach (var info in PrerequisiteFilesInfo)
            {
                var filepath = info.Key;
                var filename = Path.GetFileName(info.Key);
                var kurjunFileInfo = info.Value;
                var calculatedMd5 = Deploy.Calc_md5(filepath, upperCase: false);
                logger.Info("Checking md5: {0}.", filepath);
                StageReporter("", "Checking " + filename);
               
                //if (calculatedMd5 != kurjunFileInfo.id.Split(new [] {"."}, StringSplitOptions.None)[1])
                if (calculatedMd5 != kurjunFileInfo.id.Replace("raw.", ""))
                {
                    logger.Fatal("Verification of MD5 checksums for {0} failed. Interrupting installation.", filename);
                    Program.ShowError(
                        $"Verification of MD5 checksums for {filename} failed. Interrupting installation.", "MD5 checksums mismatch");
                       }
            }
        }
        private void unzip_extracted()
        {
            // UNZIP FILES

            Deploy.HideMarquee();

            _deploy.unzip_files(_arguments["appDir"]);
        }

        private void deploy_redist()
        {
            // DEPLOY REDISTRIBUTABLES
            StageReporter("Installing redistributables", "");
            logger.Info("Installing redistributables");
            Deploy.ShowMarquee();

            StageReporter("", "TAP driver");
            Deploy.LaunchCommandLineApp($"{_arguments["appDir"]}\\redist\\tap-driver.exe", "/S");
            logger.Info("TAP driver");

            StageReporter("", "MS Visual C++");
            Deploy.LaunchCommandLineApp($"{_arguments["appDir"]}\\redist\\vcredist64.exe", "/install /quiet");
            logger.Info("MS Visual C++");

            StageReporter("", "Chrome");
            Deploy.LaunchCommandLineApp("msiexec", $"/qn /i \"{_arguments["appDir"]}\\redist\\chrome.msi\"");
            logger.Info("Chrome");

            StageReporter("", "Virtual Box");
            Deploy.LaunchCommandLineApp($"{_arguments["appDir"]}\\redist\\virtualbox.exe", "--silent");
            Deploy.CreateShortcut(
                $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\Oracle\\VirtualBox\\VirtualBox.exe",
                $"{Environment.GetEnvironmentVariable("Public")}\\Desktop\\Oracle VM VirtualBox.lnk",
                "", true);
            Deploy.CreateShortcut(
                $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\Oracle\\VirtualBox\\VirtualBox.exe",
                $"{Environment.GetEnvironmentVariable("Public")}\\Desktop\\Oracle VM VirtualBox.lnk",
                "", true);

            Deploy.CreateShortcut(
                $"{Environment.GetEnvironmentVariable("ProgramFiles")}\\Oracle\\VirtualBox\\VirtualBox.exe",
                $"{Environment.GetEnvironmentVariable("ProgramData")}\\Microsoft\\Windows\\Start Menu\\Programs\\Oracle VM VirtualBox\\Oracle VM VirtualBox.lnk",
                "", true);
            logger.Info("Virtual Box");

        }

        private void prepare_vbox()
 {
            // PREPARE VBOX
            StageReporter("Preparing Virtual Box", "");
            //logger.Info("Preparing Virtual Box.");
            logger.Info("Preparing Virtual Box");
            Deploy.ShowMarquee();
            // prepare NAT network
            Deploy.LaunchCommandLineApp("vboxmanage", "natnetwork add --netname natnet1 --network '10.0.5.0/24' --enable --dhcp on");
            logger.Info("vboxmanage natnetwork add --netname natnet1 --network '10.0.5.0/24 --enable --dhcp on");

            // import OVAs
            StageReporter("", "Importing Snappy");
            Deploy.LaunchCommandLineApp("vboxmanage", $"import {_arguments["appDir"]}\\ova\\snappy.ova");
            logger.Info("vboxmanage import snappy.ova");

        }

        private void prepare_rh()
        {
            // PREPARE RH
            StageReporter("Preparing resource host", "");
            logger.Info("Preparing resource host");

            Deploy.ShowMarquee();

            // clone VM
            StageReporter("", "Cloning VM");
            Deploy.LaunchCommandLineApp("vboxmanage", $"clonevm --register --name {_cloneName} snappy");
            logger.Info("vboxmanage clone vm --register --name {0} snappy ", _cloneName);
            Deploy.LaunchCommandLineApp("vboxmanage", $"unregistervm --delete snappy");
            logger.Info("vboxmanage unregistervm --delete snappy");

            // prepare NIC
            StageReporter("", "Preparing NIC - NAT");
            logger.Info("Preparing NIC-NAT");
            Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {_cloneName} --nic4 none");
            //Deploy.LaunchCommandLineApp("vboxmanage",
            //    $"modifyvm {_cloneName} --nic1 nat --cableconnected1 on --natpf1 'ssh-fwd,tcp,,4567,,22' --natpf1 'mgt-fwd,tcp,,9999,,8443'");
            Deploy.LaunchCommandLineApp("vboxmanage",
                $"modifyvm {_cloneName} --nic1 nat --cableconnected1 on --natpf1 \"ssh-fwd,tcp,,4567,,22\" --natpf1 \"mgt-fwd,tcp,,9999,,8443\"");
            logger.Info("modifyvm {0} --nic1 nat --cableconnected1 on --natpf1 'ssh-fwd,tcp,,4567,,22' --natpf1 'mgt-fwd,tcp,,9999,,8443'", _cloneName);

            StageReporter("", "Setting vbox0");
            logger.Info("Setting vbox0");
            vm_vbox0();
            StageReporter("", "Setting bridged");
            logger.Info("Setting bridged");
            vm_bridged();
            // set RAM
            StageReporter("", "Setting RAM");
            
            var hostRam = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1024 / 1024;
            //ulong vmRam = 3072;
            ulong vmRam = 2048;
            if ((hostRam < 16200) && (hostRam > 8000))
            {
                vmRam = hostRam/2;
            }
            else if (hostRam > 16500)
            {
                vmRam = 8124;
            }
            Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {_cloneName} --memory {vmRam}");
            logger.Info("vboxmanage modifyvm {0} --memory {1}", _cloneName, vmRam);

            //number of cores
            StageReporter("", "Setting number of processors");
            int hostCores = Environment.ProcessorCount; //number of logical processors
            //textBox1.Text = "hostCores=" + hostCores.ToString();
            ulong vmCores = 2;
            if (hostCores > 4 && hostCores < 17) //to ensure that not > than halph phys processors will be used
            {
                vmCores = (ulong)hostCores / 2;
            } else if (hostCores >16)
            {
                vmCores = 8;
            }

            //textBox1.Text = "vmCores=" + vmCores.ToString();
            Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {_cloneName} --cpus {vmCores}");
            logger.Info("vboxmanage modifyvm {0} --cpus {1}", _cloneName, vmCores.ToString());
            // time settings
            StageReporter("", "Setting timezone");
            Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {_cloneName} --rtcuseutc on");
            logger.Info("vboxmanage modifyvm {0} --rtcuseutc", _cloneName);
            //start VM

            StageReporter("", "Starting VM");
            Deploy.LaunchCommandLineApp("vboxmanage", $"startvm --type headless {_cloneName}");
            logger.Info("vboxmanage startvm --type headless {0} ", _cloneName);
            // DEPLOY PEER
            StageReporter("Setting up peer", "");
            logger.Info("Setting up peer");
            // waiting SSH session
            StageReporter("", "Waiting for SSH");
            Deploy.WaitSsh("127.0.0.1", 4567, "ubuntu", "ubuntu");
            logger.Info("Waiting for SSH");
            // creating tmpfs folder
            StageReporter("", "Creating tmps folder");
            Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "mkdir tmpfs; mount -t tmpfs -o size=1G tmpfs/home/ubuntu/tmpfs");
            logger.Info("Creating tmpfs folder");
            // copying snap
            StageReporter("", "Copying Subutai SNAP");
            logger.Info("Copying Subutai SNAP, prepare-server.sh");
            Deploy.SendFileSftp("127.0.0.1", 4567, "ubuntu", "ubuntu", new List<string>() {
                $"{_arguments["appDir"]}/redist/subutai/prepare-server.sh",
                $"{_arguments["appDir"]}/redist/subutai/subutai_4.0.0_amd64.snap"
                }, "/home/ubuntu/tmpfs");
                         
           // adopting prepare-server.sh
            StageReporter("", "Adapting installation scripts");
            Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sed -i 's/IPPLACEHOLDER/192.168.56.1/g' /home/ubuntu/tmpfs/prepare-server.sh");
            logger.Info("Adapting installation scripts");
            // running prepare-server.sh script
            StageReporter("", "Running installation scripts");
            Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo bash /home/ubuntu/tmpfs/prepare-server.sh");
            logger.Info("Running installation scripts");
            // deploying peer options
            StageReporter("", "Setting peer options");
            logger.Info("Setting peer options");
            if (_arguments["peer"] != "rh-only")
            {
                logger.Info("trial");
                if (_arguments["peer"] == "trial")
                {
                    if (_arguments["network-installation"].ToLower() != "true")
                    {
                        // setting iptables rules
                        StageReporter("", "Restricting SSH only");
                        Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", _privateKeys,
                            "sudo iptables -P INPUT DROP; sudo iptables -P OUTPUT DROP; sudo iptables -A INPUT -p tcp -m tcp --dport 22 -j ACCEPT; sudo iptables -A OUTPUT -p tcp --sport 22 -m state --state ESTABLISHED, RELATED -j ACCEPT");
                    }


                    if (_arguments["network-installation"].ToLower() == "true")
                    {
                        // installing master template
                        StageReporter("", "Importing master");
                        logger.Info("Importing master");
                        Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo subutai -d import master 3>&2 > master_log");

                        // installing management template
                        logger.Info("Importing management");
                        StageReporter("", "Importing management");
                        Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo subutai -d import management 3?>&2 > management_log");
                    }
                    else
                    {
                        // installing master template
                        StageReporter("", "Importing master");
                        logger.Info("Importing master");
                        Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", _privateKeys, "sudo echo -e 'y' | sudo subutai -d import master 3>&2 > master_log");
                        
                        // installing management template
                        StageReporter("", "Importing management");
                        logger.Info("Importing management");
                        Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", _privateKeys, "sudo echo -e 'y' | sudo subutai -d import management 3>&2 > management_log ");
                    }

                    if (_arguments["network-installation"].ToLower() != "true")
                    {
                        // setting iptables rules
                        StageReporter("", "Allowing TCP trafic");
                        logger.Info("Allowing TCP trafic");
                        Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo iptables - P INPUT ACCEPT; sudo iptables -P OUTPUT ACCEPT");
                    }
                }
            }
            
            //check_files();
            //wait_mh("http://www.gooogle.com/");
        }

        private string vm_vbox0_ifname()
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in interfaces)
            {
                logger.Info("adapter: " + adapter.Name);
                foreach (UnicastIPAddressInformation unicast_address_info in adapter.GetIPProperties().UnicastAddresses)
                {
                    logger.Info("uucast address: " + unicast_address_info.Address.ToString());
                    if ((unicast_address_info.Address.AddressFamily == AddressFamily.InterNetwork) &&
                        (adapter.Description.ToString().Contains("Host-Only") ||
                         adapter.Name.ToString().Contains("Host-Only")))
                    {
                        if (unicast_address_info.Address.ToString() == "192.168.56.1")
                        {
                            logger.Info("vbox0 Name = {0}", adapter.Description.ToString());
                            return (adapter.Description.ToString());
                        }
                    }
                }
            }
            return ("");
        }

        private void vm_vbox0()
        {
            string netif_vbox0 = vm_vbox0_ifname();
            logger.Info("Hostonly interface name: ",netif_vbox0);
            if (netif_vbox0 == "")
            {
                //enable hostonly 
                netif_vbox0 = "VirtualBox Host-Only Ethernet Adapter";
                Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {_cloneName} --nic2 hostonly --hostonlyadapter2 \"{netif_vbox0}\"");
                Deploy.LaunchCommandLineApp("vboxmanage", " hostonlyif create ");
                Deploy.LaunchCommandLineApp("vboxmanage", $" hostonlyif ipconfig \"{netif_vbox0}\" --ip 192.168.56.1  ");
                Deploy.LaunchCommandLineApp("vboxmanage", $" dhcpserver add --ifname \"{netif_vbox0}\" --ip 192.168.56.1 --netmask 255.255.255.0 --lowerip 192.168.56.100 --upperip 192.168.56.200");
                Deploy.LaunchCommandLineApp("vboxmanage", $" hostonlyif ipconfig \"{netif_vbox0}\" --ip 192.168.56.1  ");
                Deploy.LaunchCommandLineApp("vboxmanage", $" dhcpserver modify --ifname \"{netif_vbox0}\" --enable ");
            }
            //enable hostonly 
            string res = Deploy.LaunchCommandLineApp("vboxmanage", $"modifyvm {_cloneName} --nic2 hostonly --hostonlyadapter2 \"{netif_vbox0}\"");
            logger.Info("Enable hostonly. {0}", res);
        }

        private void vm_bridged()
        {
            //stop VM
            //Deploy.LaunchCommandLineApp("vboxmanage", $"controlvm {_cloneName} poweroff soft");
            //get default routing interface
            string netif = gateway_if();
            logger.Info("Gateway interface: {0}", netif);

            //change nic1 type
            string br_cmd = $"modifyvm {_cloneName} --nic3 bridged --bridgeadapter3 \"{netif}\"";
            logger.Info("br_cmd: {0}", br_cmd);
            Deploy.LaunchCommandLineApp("vboxmanage", br_cmd);
            // start VM
            //Deploy.LaunchCommandLineApp("vboxmanage", $"startvm --type headless {_cloneName} ");
            //logger.Info("vm: {0}started", _cloneName);

        }

        private void  vm_bridged_()
        {
            Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "echo -e 'allow-hotplug eth1\niface eth1 inet dhcp' | sudo tee /writable/system-data/etc/network/interfaces.d/eth1 > /dev/null");
            Deploy.SendSshCommand("127.0.0.1", 4567, "ubuntu", "ubuntu", "sudo sync");
            
            //stop VM
            Deploy.LaunchCommandLineApp("vboxmanage", $"controlvm {_cloneName} poweroff soft");
            //get default routing interface
            string netif = gateway_if();
            logger.Info("Gateway interface: {0}", netif);
  
            //change nic1 type
            string br_cmd = $"modifyvm {_cloneName} --nic1 bridged --bridgeadapter1 \"{netif}\"";
            //MessageBox.Show("cmd:" + br_cmd, "bridge", MessageBoxButtons.OK);
            logger.Info("br_cmd: {0}", br_cmd);
            Deploy.LaunchCommandLineApp("vboxmanage", br_cmd);
            Deploy.LaunchCommandLineApp("vboxmanage",
                $"modifyvm {_cloneName} --nic4 nat --cableconnected4 on --natpf4 'ssh-fwd,tcp,,4567,,22' --natpf4 'mgt-fwd,tcp,,9999,,8443\'");//mgt-fwd

             // start VM
            Deploy.LaunchCommandLineApp("vboxmanage", $"startvm --type headless {_cloneName} ");
            logger.Info("vm: {0}started", _cloneName);
            
            //sudo - u $(users) $vboxmanage startvm --type headless subutai
        }

        private string gateway_if()
        {
            var gateway_address = NetworkInterface.GetAllNetworkInterfaces()
                .Where(e => e.OperationalStatus == OperationalStatus.Up)
                .SelectMany(e => e.GetIPProperties().GatewayAddresses)
                .FirstOrDefault();

            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface adapter in interfaces)
            {
                if (adapter.OperationalStatus.ToString() != "Up")
                    continue;
                foreach (UnicastIPAddressInformation unicast_address_info in adapter.GetIPProperties().UnicastAddresses)
                {
                    if ((unicast_address_info.Address.AddressFamily == AddressFamily.InterNetwork) &&
                        !(
                            adapter.Description.ToString().Contains("Virtual") ||
                            adapter.Description.ToString().Contains("Pseudo") ||
                            adapter.Description.ToString().Contains("Software") ||
                            adapter.Name.ToString().Contains("Virtual")
                            )
                        )

                    //&& (unicast_address_info.Address.AddressFamily != AddressFamily.))
                    //ip.AddressFamily == AddressFamily.InterNetwork))
                    {

                        IPAddress mask = unicast_address_info.IPv4Mask;
                        //MessageBox.Show("ip=" + unicast_address_info.Address.ToString() + "gw=" + adapter.GetIPProperties().GatewayAddresses.FirstOrDefault().Address.ToString(), adapter.Name.ToString(), MessageBoxButtons.OK);
                        if (IsInSameSubnet(unicast_address_info.Address, gateway_address.Address, mask) &&
                            adapter.GetIPProperties().GatewayAddresses.FirstOrDefault().Address.ToString() == gateway_address.Address.ToString())
                        {
                            //MessageBox.Show("return ip=" + unicast_address_info.Address.ToString(), adapter.Description.ToString(), MessageBoxButtons.OK);
                            return adapter.Description.ToString();
                        }
                    }
                  }
               }
            return null;
          }
       

        private IPAddress GetNetworkAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));
            }
            return new IPAddress(broadcastAddress);
        }


        private bool IsInSameSubnet(IPAddress address2, IPAddress address1, IPAddress subnetMask)
        {
            IPAddress network1 = GetNetworkAddress(address1, subnetMask);
            IPAddress network2 = GetNetworkAddress(address2, subnetMask);

            return network1.Equals(network2);
        }
        private void deploy_p2p()
        {
            // DEPLOYING P2P SERVICE
            StageReporter("Installing P2P service", "");
            Deploy.ShowMarquee();

            var name = "Subutai Social P2P";
            var binPath = $"{_arguments["appDir"]}bin\\p2p.exe";
            const string binArgument = "daemon";

            // installing service
            StageReporter("", "Installing P2P service");
            Deploy.LaunchCommandLineApp("nssm", $"install \"{name}\" \"{binPath}\" \"{binArgument}\"");
            //logger.Info("Installing P2P service ");
            // starting service
            StageReporter("", "Starting P2P service");
            Deploy.LaunchCommandLineApp("nssm", $"start \"{name}\"");
            //logger.Info("Starting P2P service");
        }

        #endregion

        private void Form1_VisibleChanged(object sender, EventArgs e)
        {
            if (((Form1)sender).Visible == false)
                Program.form2.Show();
        }

        private void check_files()
        {
            StageReporter("", "Performing postinstall check");
            //logger.Info("Postinstall cjeck");
            //MessageBox.Show(_arguments["repo_tgt"], "_args", MessageBoxButtons.OK);
            //StageReporter("", "Download target description file");
            download_description_file("repo_tgt");
            //logger.Info("Downloading description file repotgt");
            //var rows = File.ReadAllLines("C:\\Subutai\\repotgt");
            String pth = $"{_arguments["appDir"]}/{_arguments["repo_tgt"]}";
            //logger.Info("path {0}", pth);
            var rows = File.ReadAllLines(pth);
            //MessageBox.Show(pth + ": " + rows.ToString(), "rows", MessageBoxButtons.OK);
            foreach (var row in rows)
            {
                var folderFile = row.Split(new[] { "|" }, StringSplitOptions.None);
                var folderpath = folderFile[0].Trim();
                var filename = folderFile[1].Trim();
                String fullFolderPath = $"{_arguments["appDir"]}/{folderpath.ToString()}";
                String fullFileName = $"{_arguments["appDir"]}/{folderpath.ToString()}/{filename.ToString()}";
                //StageReporter("", folderpath.ToString() + "/" + filename.ToString());
                //logger.Info("file {0}/{1}", fullFolderPath, filename);
                MessageBox.Show(folderpath.ToString() + "/" + filename.ToString(), "file-folder", MessageBoxButtons.OK);

                if (!Directory.Exists(fullFolderPath))
                {
                    MessageBox.Show("We are sorry, but something was wrong with Subutai installation. \nFolder" +  fullFolderPath + "does not exist. \nUninstall Subutai from Control Panel, turn off all antivirus software, firewalls and SmartScreen and try again.", "Folder not exist", MessageBoxButtons.OK);
                    //logger.Info("Directory {0} not found.", fullFolderPath);
                    Environment.Exit(1);
                }
                if (!File.Exists(fullFileName))
                {
                    MessageBox.Show("We are sorry, but something was wrong with Subutai installation. \nFile " + fullFileName + " does not exist. \n\nUninstall Subutai from Control Panel, turn off all antivirus software, firewalls and SmartScreen and try again.", "Folder not exist", MessageBoxButtons.OK);
                    //logger.Info("file {0}/{1} not found.", fullFolderPath, filename);
                    Environment.Exit(2);
                }
            }
        }

        private async Task<int> wait_mh(string strUrl)
        {
            HttpWebRequest webReq = (HttpWebRequest)WebRequest
                                           .Create(strUrl);
            webReq.AllowAutoRedirect = false;
            //HttpWebResponse response = (HttpWebResponse)webReq.GetResponse();
            //Returns "MovedPermanently", not 301 which is what I want.
            HttpWebResponse wResp;
            int statusCode = 0;


            while (statusCode != 200) {
                try
                {
                    wResp = (HttpWebResponse)webReq.GetResponse();
                    statusCode = (int)wResp.StatusCode;
                }
                catch (WebException we)
                {
                    statusCode = (int)((HttpWebResponse)we.Response).StatusCode;
                    //Thread.Sleep(1000);
                }
                await Task.Delay(2000);
                logger.Info("Response = {0}", statusCode.ToString());
            }

            return (statusCode);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _deploy.SetEnvironmentVariables();

            if (_arguments["network-installation"].ToLower() == "true")
            {
                //DOWNLOAD REPO
                StageReporter("Downloading prerequisites", "");

                Deploy.HideMarquee();
                download_repo();
            }

            //check_files();
            //int x = wait_mh("http://www.gooogle.com/");
            //MessageBox.Show("resonse returned " + x.ToString(), "info", MessageBoxButtons.OK);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Invoke((MethodInvoker) Refresh);
        }
    }
}
