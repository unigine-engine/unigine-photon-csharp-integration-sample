#if UNIGINE_DOUBLE
using Vec3 = Unigine.dvec3;
using Vec4 = Unigine.dvec4;
using Mat4 = Unigine.dmat4;
#else
using Vec3 = Unigine.vec3;
using Vec4 = Unigine.vec4;
using Mat4 = Unigine.mat4;
#endif

using Unigine;
using Photon.Chat;
using System.Runtime.InteropServices;
using ExitGames.Client.Photon;
using System.Linq;
using System;

[Component(PropertyGuid = "3ec7eb7c96315bfc1f6237e5b00c043cd6b6f6aa")]
public class GameChat : Component, IChatClientListener
{
	ChatClient chat_client = null;

	WidgetWindow chat_window = null;
	WidgetScrollBox scrollbox = null;
	WidgetEditLine editline = null;
	WidgetButton send_button = null;

	string appID = "no-app-id";
	string appVersion = "1.0";

	private void Init()
	{

		var application_params = new Json();
		application_params.Load("application_params.json");

		appID = application_params.GetChild("chat_application_id").GetString();
		appVersion = application_params.GetChild("chat_application_version").GetString();

		if (appID == "no-app-id")
		{
			Log.ErrorLine("No chat appID in application_params.json, unable to connect to the server!");
		}

		chat_client = new ChatClient(this);

		var username = (Engine.GetSystemLogic(0) as UnigineApp.AppSystemLogic).GetPlayerName();

		chat_client.Connect(appID, appVersion, new AuthenticationValues()
		{
			UserId = username
		});

		var gui = WindowManager.MainWindow.Gui;

		chat_window = new WidgetWindow(gui);
		chat_window.Width = 300;
		chat_window.Height = 500;
		chat_window.Moveable = true;
		chat_window.Sizeable = false;
		chat_window.MaxWidth = 300;
		chat_window.MaxHeight = 500;

		WindowManager.MainWindow.AddChild(chat_window, Gui.ALIGN_OVERLAP);

		var info_label = new WidgetLabel("This sample is a simplified example of Photon integration with UNIGINE.<br><br>" +
			"Use <b>WASD</b> buttons to move, <b>QE</b> to rotate, <b>LMB</b> to shoot other players.<br><br>" +
			"As the life progress bar is empty, the Leave button is displayed on the screen.Click it to return to Lobby.<br><br>" +
			"In the message box, you can type in messages to send them to all users, or send messages starting with <b>@username</b> to exchange private messages with that user.<br> ");

		info_label.FontWrap = 1;
		info_label.FontRich = 1;

		chat_window.AddChild(info_label, Gui.ALIGN_EXPAND);

		scrollbox = new WidgetScrollBox(gui, 0, 10);
		scrollbox.HScrollEnabled = false;
		chat_window.AddChild(scrollbox, Gui.ALIGN_TOP | Gui.ALIGN_EXPAND);

		var hbox = new WidgetHBox(gui);
		hbox.Width = 300;

		editline = new WidgetEditLine(gui);
		hbox.AddChild(editline, Gui.ALIGN_LEFT | Gui.ALIGN_EXPAND);

		send_button = new WidgetButton(gui, "Send");
		send_button.EventClicked.Connect(SendMessage);
		hbox.AddChild(send_button, Gui.ALIGN_LEFT);

		chat_window.AddChild(hbox, Gui.ALIGN_BOTTOM);
	}

	private void Update()
	{
		chat_client.Service();

		if (Input.IsKeyDown(Input.KEY.ENTER))
			SendMessage();
	}

	private void Shutdown()
	{
		string[] chs = new string[1];
		chs[0] = "a";
		chat_client.Unsubscribe(chs);
		chat_client.Disconnect();

		chat_client = null;

		chat_window.DeleteLater();
	}

	private void AddMessage(string sender, string message)
	{
		var hbox = new WidgetHBox();
		hbox.Width = scrollbox.Width - 20;

		var label_sender = new WidgetLabel(sender);
		label_sender.Arrange();

		hbox.AddChild(label_sender, Gui.ALIGN_LEFT | Gui.ALIGN_TOP);

		var message_vbox = new WidgetVBox();

		var label_message = new WidgetLabel(message);
		label_message.FontWrap = 1;
		label_message.Width = hbox.Width - label_sender.Width;
		label_message.Arrange();
		while (label_message.Width > hbox.Width - label_sender.Width)
		{
			int count = label_message.Width / (hbox.Width - label_sender.Width) + 1;
			string str = label_message.Text;
			label_message.Text = str.Substring(0, str.Length / count - 1);

			message_vbox.AddChild(label_message, Gui.ALIGN_LEFT);

			label_message = new WidgetLabel(str.Substring(str.Length / count - 1));
			label_message.FontWrap = 1;
			label_message.Width = hbox.Width - label_sender.Width;
			label_message.Arrange();
		}

		message_vbox.AddChild(label_message, Gui.ALIGN_LEFT);

		hbox.AddChild(message_vbox, Gui.ALIGN_LEFT | Gui.ALIGN_TOP);

		scrollbox.AddChild(hbox, Gui.ALIGN_LEFT);

		scrollbox.Arrange();
	}

	private void SendMessage()
	{
		string buffer = editline.Text;
		editline.Text = "";

		if (buffer.Length == 0)
		{
			return;
		}

		if (buffer[0] == '@')
		{
			int sep = buffer.IndexOf(" ", 0);
			chat_client.SendPrivateMessage(buffer.Substring(1, sep - 1), buffer.Substring(sep + 1));
		}
		else
		{
			if (chat_client.PublicChannels.Count > 0)
			{
				chat_client.PublishMessage(chat_client.PublicChannels.FirstOrDefault().Value.Name, buffer);
			}
		}
	}

	public void DebugReturn(DebugLevel level, string message)
	{
		Log.MessageLine(message);
	}

	public void OnChatStateChange(ChatState state)
	{
		Log.MessageLine("OnStateChange " + state.ToString());
	}

	public void OnConnected()
	{
		Log.MessageLine("Connected");
		string[] chs = new string[1];
		chs[0] = "a";
		if (chat_client.Subscribe(chs))
			Log.MessageLine("Subscribing");
	}

	public void OnDisconnected()
	{
		Log.MessageLine("Disconnected");
	}

	public void OnGetMessages(string channelName, string[] senders, object[] messages)
	{
		for(int i =0; i < senders.Length; i++)
		{
			AddMessage("[" + senders[i] + "] >>> ", messages[i].ToString());
		}
	}

	public void OnPrivateMessage(string sender, object message, string channelName)
	{
		AddMessage("[Private " + sender + " to " + channelName + "] >>> ", message.ToString());
	}

	public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
	{
		Log.MessageLine("OnStatusUpdate " + user + " " + status.ToString() + " " + (gotMessage ? message.ToString() : " "));
	}

	public void OnSubscribed(string[] channels, bool[] results)
	{
		Log.MessageLine("OnSubscribed " + channels.ToString() + ", " + results.ToString());
	}

	public void OnUnsubscribed(string[] channels)
	{
		Log.MessageLine("OnUnsubscribed " + channels.ToString());
	}

	public void OnUserSubscribed(string channel, string user)
	{
		Log.MessageLine("OnUserSubscribed " + channel + " " + user);
	}

	public void OnUserUnsubscribed(string channel, string user)
	{
		Log.MessageLine("OnUserUnsubscribed " + channel + " " + user);
	}
}