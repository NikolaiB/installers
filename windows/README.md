# Subutai Windows Installer

# How to install
Run installer w/Administrative privileges on fresh Windows 7/8/10 x64 machine </br>
Wait until installation process and shell scripts finish installation </br>
Now you can go to Chrome browser and open management dashbord https://localhost:11443 </br>


# Setup environment
You need the following tools to build the installer:
	<ul>
	<li> Advanced Installer 12 </li>
	<li> VMware Player or Workstation 12 </li>
	<li> Windows 10 x64 VMware image </li>
	<li> Windows 8 x64 VMware image </li>
	<li> Windows 7 x64 VMware image </li>
	</ul>

# Build the installer
	Simply run the Build process from Advanced installer
	You can use CLI for this purpose: http://www.advancedinstaller.com/user-guide/command-line.html

# Test the installer
	You can Build and Run the installer inside VM right from Advanced installer

# Structure of folders
	Default application path is "C:\Subutai"
	./bin
		./tray
	./extensions
		./pgp
	./home
		./user
			./.ssh
	./ova
	./redist
		./subutai
	./templates

	./bin 				- all scripts and binaries
	./bin/tray 			- tray application binaries
	./extensions 		- extensions for browsers
	./extensions/pgp 	- HUB extension for Chrome browser
	./home 				- cygwin-related folder (sshpass, scp and ssh are cygwin-based and looking at this folder)
	./home/user/.ssh 	- SSH keys
	./ova 				- VirtualBox images
	./redist 			- Redistributables
	./redist/subutai 	- Subutai-specific redistributables
	./templates 		- Subutai templates

# Description of Files
	./bin/autodeploy.ps1 	- Powershell script responsible for deployment of redistributables and initalization of Subutai environment
	./bin/deploy.bat 		- wrapper for autdeploy.ps1
	./bin/nssm.exe 			- manager of Windows services
	./bin/p2p.exe 			- Subutai P2P binary
	./bin/scp.exe 			- cygwin-based analog of Linux scp command
	./bin/ssh.exe 			- cygwin-based ssh command
	./bin/ssh-keygen.exe 	- cygwin-based ssh-keygen command
	./bin/sshpass.exe 		- cygwin-based sshpass command
	./bin/uninstall-clean.bat - wrapper for autodeploy.ps1 w/keys to clean crap after uninstall

	./bin/tray/SubutaiTray.exe - Subutai Tray application

	./ova/snappy.ova - image of Ubuntu Snappy

	./redist/chrome.msi 	- Chrome Browser
	./redist/tap-driver.exe - TAP driver
	./redist/vcredist64.exe - MS Visual C++ Runtime libraries
	./redist/virtualbox.exe - VirtualBox

	./redist/subutai/prepare-server.sh 			- script for deployment of Subutai inside Snappy
	./redist/subutai/subutai_4.0.0_amd64.snap 	- SNAP of Subutai

	./templates/management-subutai-template_4.0.0_amd64.tar.gz 	- Subutai master template
	./templates/master-subutai-template_4.0.0_amd64.tar.gz 		- Subutai management template