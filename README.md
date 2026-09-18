# LPU+ Remote Support Platform

**LPU+** is a cutting-edge web-based remote desktop platform with integrated AI. It allows you to control any Windows, Mac, or Linux computer directly from your web browser, complete with an AI assistant that can instantly analyze the screen!

* **Web URL**: [https://lpuplus-phoaplcl0-yatish-kumars-projects-23269364.vercel.app](https://lpuplus-phoaplcl0-yatish-kumars-projects-23269364.vercel.app)

---

## 🚀 How to Install and Run the Agent

To share a screen and allow remote control, you must install the **LPU+ Agent** on the "Host" computer.

We've made this a true **"Zero-Install"** experience! You don't need to manually download anything; just copy and paste the single command below for your operating system. The script will automatically install any dependencies, download the agent, and start it in the background!

### For Windows Users

1. Open **PowerShell** (Press `Win + R`, type `powershell`, and hit Enter).
2. Paste the following command and hit Enter:

```powershell
irm https://raw.githubusercontent.com/lpu-software/LPUplus/main/install.ps1 | iex
```

3. Wait a few moments. Once the installation is complete, it will print out your **8-character Pairing Code** (e.g., `ABCD-1234`).

### For Mac & Linux Users

1. Open **Terminal** (Press `Cmd + Space`, type `Terminal`, and hit Enter).
2. Paste the following command and hit Enter:

```bash
curl -sSL https://raw.githubusercontent.com/lpu-software/LPUplus/main/install.sh | bash
```

3. Wait a few moments. Once the installation is complete, it will print out your **8-character Pairing Code** (e.g., `ABCD-1234`).

---

## 🔗 How to Connect

1. Go to the LPU+ Web App: [https://lpuplus-phoaplcl0-yatish-kumars-projects-23269364.vercel.app](https://lpuplus-phoaplcl0-yatish-kumars-projects-23269364.vercel.app)
2. Enter the **Pairing Code** generated from the step above.
3. You now have full remote desktop control directly from your browser!

### AI Assistant (@screen)
If you need help identifying an error on your screen or reading a document, open the chat sidebar in the web app. Simply type **`@screen`** in your message, and the AI will analyze the remote screen instantly.

---

## 🛑 How to Stop the Agent

The agent runs silently in the background. If you want to stop sharing your screen, you can stop the agent at any time.

**On Windows:**
Open PowerShell and run:
```powershell
cd ~/.lpuplus
.\run-agent.ps1 stop
```

**On Mac/Linux:**
Open Terminal and run:
```bash
cd ~/.lpuplus
./run-agent.sh stop
```

*(You can also use the `status` or `log` commands to check if it's running or to view your pairing code again).*
