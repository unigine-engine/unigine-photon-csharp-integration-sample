using ExitGames.Client.Photon;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

namespace UnigineApp
{
	public class AppSystemLogic : SystemLogic
	{
        private LoadBalancingClient network_client = new LoadBalancingClient();
        private MainMenu menu = null;

        public event Action<int, Mat4> OnEventTransform;
        public event Action<int, Vec3, Vec3> OnEventShot;
        public event Action<int, int> OnEventHP;

        private string appID = "no-ap-id";
        private string appVersion = "1.0";

        public override bool Init()
        {
            Engine.BackgroundUpdate = Engine.BACKGROUND_UPDATE.BACKGROUND_UPDATE_RENDER_NON_MINIMIZED;
            ComponentSystem.Enabled = true;

            Utils.PhotonVec3.RegisterSerializable();
            Utils.PhotonMat4.RegisterSerializable();

            var application_params = new Json();

            application_params.Load("application_params.json");

            appID = application_params.GetChild("realtime_application_id").GetString();
            appVersion = application_params.GetChild("realtime_application_version").GetString();

            if (appID == "no-app-id")
            {
                Log.ErrorLine("No realtime appID in application_params.json, unable to connect to the server!");
            }

            network_client.AddCallbackTarget(this);
            network_client.StateChanged += OnStateChange;
            network_client.EventReceived += OnEventReceived;

            Unigine.Console.Run("world_load menu_world");
            menu = new MainMenu(this);
            network_client.AddCallbackTarget(menu);

            return true;
        }

        public override bool Update()
        {
            network_client.Service();

            return true;
        }

        public override bool Shutdown()
        {
            network_client.Disconnect();
            network_client.RemoveCallbackTarget(this);

            if (menu != null)
            {
                network_client.RemoveCallbackTarget(menu);
                menu.Delete();
            }

            menu = null;

            return true;
        }

        public void AddCallbackTarget(object target)
        {
            network_client.AddCallbackTarget(target);
        }

        public void RemoveCallbackTarget(object target)
        {
            network_client.RemoveCallbackTarget(target);
        }

        private void OnEventReceived(EventData data)
        {
            int senderNumber = data.Sender;

            switch ((Utils.EventData)data.Code)
            {
                case Utils.EventData.Transform:
                    Utils.PhotonMat4 transform = (Utils.PhotonMat4)(data.CustomData);
                    OnEventTransform?.Invoke(senderNumber, transform.mat);
                    break;
                case Utils.EventData.Shot:
                    Hashtable values = (Hashtable)data.CustomData;
                    Utils.PhotonVec3 start = (Utils.PhotonVec3)values["start"];
                    Utils.PhotonVec3 direction = (Utils.PhotonVec3)values["direction"];
                    OnEventShot?.Invoke(senderNumber, start.vec, direction.vec);
                    break;
                case Utils.EventData.HP:
                    int hp = (int)data.CustomData;
                    OnEventHP?.Invoke(senderNumber, hp);
                    break;
            }
        }

        private void OnStateChange(ClientState arg1, ClientState arg2)
        {
            Log.MessageLine(arg1 + " -> " + arg2);

            if (arg2 == ClientState.ConnectedToMasterServer)
            {
                TypedLobby lobby = new TypedLobby("lobby", LobbyType.Default);
                network_client.OpJoinLobby(lobby);
            }

            if (arg2 == ClientState.Joined)
                RoomJoined();
        }

        public void Connect(string nickname)
        {
            bool ans = network_client.ConnectUsingSettings(new AppSettings()
            {
                AppIdRealtime = appID,
                AppVersion = appVersion
            });

            network_client.NickName = nickname;
        }

        private void RoomJoined()
        {
            network_client.RemoveCallbackTarget(menu);
            menu.Delete();
            menu = null;

            Unigine.Console.Run("world_load room_world");
        }

        public void CreateRoom(string gameID)
        {
            network_client.OpCreateRoom(new EnterRoomParams()
            {
                RoomName = gameID,
                RoomOptions = new RoomOptions()
                {
                    PlayerTtl = int.MaxValue,
                    EmptyRoomTtl = 0
                }
            });
        }

        public void JoinRoom(string gameID)
        {
            network_client.OpJoinRoom(new EnterRoomParams()
            {
                RoomName = gameID
            });
        }

        public void LeaveRoom()
        {
            network_client.OpLeaveRoom(true);

            Unigine.Console.Run("world_load menu_world");

            menu = new MainMenu(this);
            network_client.AddCallbackTarget(menu);
        }

        public void Disconnect()
        {
            network_client.Disconnect();
        }

        public List<int> GetRoomPlayersNumbers()
        {
            List<int> players = new List<int>();
            var map = network_client.CurrentRoom.Players;
            foreach (var it in map)
            {
                if (it.Value.IsInactive == false)
                    players.Add(it.Key);
            }

            return players;
        }

        public int GetPlayerNumber()
        {
            return network_client.LocalPlayer.ActorNumber;
        }

        public string GetPlayerName()
        {
            return network_client.NickName;
        }

        public void SendEventTransform(Mat4 transform)
        {
            network_client.OpRaiseEvent((byte)Utils.EventData.Transform,
                new Utils.PhotonMat4(transform),
                new RaiseEventOptions(),
                SendOptions.SendUnreliable);
        }

        public void SendEventShot(Vec3 start, Vec3 direction)
        {
            Hashtable values = new Hashtable();
            values.Add("start", new Utils.PhotonVec3(start));
            values.Add("direction", new Utils.PhotonVec3(direction));

            network_client.OpRaiseEvent((byte)Utils.EventData.Shot,
                values,
                new RaiseEventOptions(),
                SendOptions.SendReliable);
        }

        public void SendEventHP(int hp)
        {
            network_client.OpRaiseEvent((byte)Utils.EventData.HP,
                hp,
                new RaiseEventOptions(),
                SendOptions.SendReliable);
        }

        public bool IsConnected()
        {
            return network_client.IsConnectedAndReady;
        }
    }

    public class MainMenu : ILobbyCallbacks
    {
        private WidgetWindow authentication_window = null;
        private WidgetWindow lobby_window = null;
        private WidgetEditLine nickname_editline = null;
        private WidgetScrollBox rooms_scrollbox = null;

        AppSystemLogic network_logic = null;

        public MainMenu(AppSystemLogic instance)
        {
            network_logic = instance;

            authentication_window = new WidgetWindow("Authentication");
            authentication_window.Width = 300;
            authentication_window.Height = 400;

            var nickname_label = new WidgetLabel("Nickname:");
            nickname_label.FontSize = 28;

            nickname_editline = new WidgetEditLine();
            nickname_editline.FontSize = 28;

            var vbox = new WidgetVBox();
            vbox.Width = 300;
            vbox.AddChild(nickname_label, Gui.ALIGN_LEFT);
            vbox.AddChild(nickname_editline, Gui.ALIGN_EXPAND);

            authentication_window.AddChild(vbox, Gui.ALIGN_TOP);

            var join_button = new WidgetButton("Join Lobby");
            join_button.EventClicked.Connect(() => { network_logic.Connect(nickname_editline.Text); });
            join_button.FontSize = 30;

            vbox = new WidgetVBox();
            vbox.AddChild(join_button, Gui.ALIGN_CENTER);

            authentication_window.AddChild(vbox, Gui.ALIGN_EXPAND);

            lobby_window = new WidgetWindow("Lobby");
            lobby_window.Width = 500;
            lobby_window.Height = 300;

            rooms_scrollbox = new WidgetScrollBox();
            rooms_scrollbox.Width = 500;
            rooms_scrollbox.Height = 250;
            rooms_scrollbox.HScrollEnabled = false;

            lobby_window.AddChild(rooms_scrollbox, Gui.ALIGN_TOP);

            var gridbox = new WidgetGridBox(2);
            var leave_lobby_button = new WidgetButton("Leave");
            //TODO leave logic
            leave_lobby_button.EventClicked.Connect(LeaveLobby);
            gridbox.AddChild(leave_lobby_button, Gui.ALIGN_LEFT);

            var create_room_button = new WidgetButton("Create Room");
            //TODO create room logic
            create_room_button.EventClicked.Connect(() => { network_logic.CreateRoom(nickname_editline.Text + "1111"); });
            gridbox.AddChild(create_room_button, Gui.ALIGN_RIGHT);

            lobby_window.AddChild(gridbox, Gui.ALIGN_EXPAND);

            if (network_logic.IsConnected())
                WindowManager.MainWindow.AddChild(lobby_window, Gui.ALIGN_CENTER);
            else
                WindowManager.MainWindow.AddChild(authentication_window, Gui.ALIGN_CENTER);
        }

        public void Delete()
        {
            authentication_window.DeleteLater();
            lobby_window.DeleteLater();
        }

        public void OnJoinedLobby()
        {
            Connected();
        }

        public void OnLeftLobby() { }

        public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics)
        {
            Log.MessageLine(lobbyStatistics[0].RoomCount);
        }

        public void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            int num = rooms_scrollbox.NumChildren;
            for (int i = 0; i < num; i++)
            {
                var child = rooms_scrollbox.GetChild(0);
                rooms_scrollbox.RemoveChild(child);
                child.DeleteLater();
            }

            for (int i = 0; i < roomList.Count; i++)
            {
                var room_label = new WidgetLabel(roomList[i].Name);
                room_label.FontSize = 20;
                string name = roomList[i].Name;
                room_label.EventDoubleClicked.Connect(() => { network_logic.JoinRoom(name); });
                rooms_scrollbox.AddChild(room_label, Gui.ALIGN_TOP | Gui.ALIGN_EXPAND);
            }
        }

        private void Connected()
        {
            WindowManager.MainWindow.RemoveChild(authentication_window);
            WindowManager.MainWindow.AddChild(lobby_window, Gui.ALIGN_CENTER);
        }

        private void LeaveLobby()
        {
            network_logic.Disconnect();
            WindowManager.MainWindow.RemoveChild(lobby_window);
            WindowManager.MainWindow.AddChild(authentication_window, Gui.ALIGN_CENTER);
        }
    }

    public class GamePlayer
    {
        private NodeDummy root_node = null;
        private ObjectBillboards hp_billboard;
        private const int max_hp = 5;
        private int current_hp;
        private float movement_speed = 3.0f;
        private float rotation_speed = 30.0f;
        private bool alive = true;

        public event Action OnDead;

        public GamePlayer()
        {
            root_node = new NodeDummy();

            var ball = World.LoadNode("material_ball/material_ball.node");

            root_node.AddChild(ball);

            hp_billboard = new ObjectBillboards();
            hp_billboard.AddBillboard(2.0f, 0.1f);

            root_node.AddChild(hp_billboard);
            hp_billboard.Position = new Vec3(0.0f, 0.0f, 1.2f);

            var hp_material = Materials.FindMaterialByPath("hp_material.mat");

            hp_billboard.SetMaterial(hp_material, "*");

            current_hp = max_hp;

            alive = true;
        }

        ~GamePlayer()
        {
            root_node.DeleteLater();
            alive = false;
        }

        public void Move(Vec3 direction)
        {
            if (alive)
            {
                root_node.Translate(direction * movement_speed * Game.IFps);
            }
        }

        public void Rotate(int direction)
        {
            if (alive)
            {
                root_node.Rotate(new quat(vec3.UP, rotation_speed * Game.IFps * direction));

            }
        }

        public void SetWorldTransform(Mat4 transform)
        {
            if (alive)
            {
                root_node.WorldTransform = transform;
            }
        }

        public Mat4 GetWorldTransform()
        {
            return alive ? root_node.WorldTransform : Mat4.IDENTITY;
        }

        public Vec3 GetWorldDirection()
        {
            return alive ? root_node.GetWorldDirection(MathLib.AXIS.Y) : Vec3.ZERO;
        }

        public Vec3 GetShotPivot()
        {
            return alive ? root_node.WorldPosition + new Vec3(0.0f, 0.0f, 0.5f) : Vec3.ZERO;
        }

        public Node GetRootNodeDummy()
        {
            return root_node;
        }

        public void SetHP(int value)
        {
            current_hp = value;

            if (current_hp > 0)
            {
                hp_billboard.SetWidth(0, 2 * current_hp * 1.0f / max_hp);
            }
            else
            {
                root_node.Enabled = false;

                alive = false;

                OnDead?.Invoke();
            }
        }

        public int Damage()
        {
            SetHP(current_hp - 1);

            return current_hp;
        }

        public bool IsAlive()
        {
            return alive;
        }

        public void Kill()
        {
            root_node.Enabled = false;
            alive = false;
            current_hp = 0;

            OnDead?.Invoke();
        }
    }
}
