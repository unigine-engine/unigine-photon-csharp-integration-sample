#if UNIGINE_DOUBLE
using Vec3 = Unigine.dvec3;
using Vec4 = Unigine.dvec4;
using Mat4 = Unigine.dmat4;
#else
using Vec3 = Unigine.vec3;
using Vec4 = Unigine.vec4;
using Mat4 = Unigine.mat4;
#endif

using System.Collections.Generic;
using Unigine;
using static Unigine.Console;
using Photon.Realtime;
using System.Collections;

[Component(PropertyGuid = "6d7b166e41c003c9da8d447ba9e554d6f8618c9e")]
public class GameManager : Component, IInRoomCallbacks
{
	private UnigineApp.AppSystemLogic network_logic = null;
	private Dictionary<int, UnigineApp.GamePlayer> players = new Dictionary<int, UnigineApp.GamePlayer>();
	private UnigineApp.GamePlayer main_player;
	private Random randomizer;

	private WidgetButton leave_button;


	private float last_shot_time = 0;
	private float reload_time = 1;

	private bool leaved = false;

	private void Init()
	{
		Visualizer.Enabled = true;

		network_logic = Engine.GetSystemLogic(0) as UnigineApp.AppSystemLogic;
		network_logic.AddCallbackTarget(this);
		network_logic.OnEventTransform += OnTransform;
		network_logic.OnEventShot += OnShot;
		network_logic.OnEventHP += OnHP;

		var room_players = network_logic.GetRoomPlayersNumbers();
		for(int i = 0; i < room_players.Count; i++)
		{
			UnigineApp.GamePlayer game_player = new UnigineApp.GamePlayer();

			if (room_players[i] == network_logic.GetPlayerNumber())
			{
				var player = Game.Player as PlayerPersecutor;

				player.Fixed = true;
				player.Target = game_player.GetRootNodeDummy();
				player.MinDistance = 5.0f;
				player.MaxDistance = 12.0f;
				player.Position = new Vec3(0.0f, -10.0f, 6.0f);

				game_player.SetWorldTransform(MathLib.Translate(new Vec3(randomizer.Float(-15.0f, 15.0f), randomizer.Float(-15.0f, 15.0f), 0.0f)));

				main_player = game_player;

				main_player.OnDead += OnMainPlayerDead;
			}
			else
			{
				players.Add(room_players[i], game_player);
			}
		}

		last_shot_time = Game.Time - reload_time;

		leave_button = new WidgetButton("Leave");
		leave_button.FontSize = 32;

		leave_button.EventClicked.Connect(OnLeaveRoom);
	}

	private void Update()
	{
		if (leaved)
		{
			return;
		}

		//update input
		Vec3 movement = new Vec3(0.0f, 0.0f, 0.0f);

		if ((Input.IsKeyPressed(Input.KEY.W) || Input.IsKeyPressed(Input.KEY.UP)) && Input.MouseGrab)
		{
			movement += new Vec3(0.0f, 1.0f, 0.0f);
		}

		if ((Input.IsKeyPressed(Input.KEY.S) || Input.IsKeyPressed(Input.KEY.DOWN)) && Input.MouseGrab)
		{
			movement += new Vec3(0.0f, -1.0f, 0.0f);
		}

		if ((Input.IsKeyPressed(Input.KEY.A) || Input.IsKeyPressed(Input.KEY.LEFT)) && Input.MouseGrab)
		{
			movement += new Vec3(-1.0f, 0.0f, 0.0f);
		}

		if ((Input.IsKeyPressed(Input.KEY.D) || Input.IsKeyPressed(Input.KEY.RIGHT)) && Input.MouseGrab)
		{
			movement += new Vec3(1.0f, 0.0f, 0.0f);
		}

		if (movement.Length2 > MathLib.EPSILON)
		{
			movement.Normalize();
		}


		int rotation = 0;

		if (Input.IsKeyPressed(Input.KEY.Q) && Input.MouseGrab)
		{
			rotation += 1;
		}

		if (Input.IsKeyPressed(Input.KEY.E) && Input.MouseGrab)
		{
			rotation += -1;
		}

		bool shot = false;
		if (Input.IsMouseButtonPressed(Input.MOUSE_BUTTON.LEFT) && Game.Time - last_shot_time > reload_time && Input.MouseGrab)
		{
			shot = true;
			last_shot_time = Game.Time;
		}

		//serve input
		main_player.Move(movement);
		main_player.Rotate(rotation);

		if (shot)
		{
			Visualizer.RenderLine3D(main_player.GetShotPivot(),
				main_player.GetShotPivot() + main_player.GetWorldDirection() * 1000, vec4.RED, 0.1f);
		}

		//send transform
		network_logic.SendEventTransform(main_player.GetWorldTransform());
		if (shot)
		{
			network_logic.SendEventShot(main_player.GetShotPivot(), main_player.GetWorldDirection());
		}
	}

	private void Shutdown()
	{
		network_logic.RemoveCallbackTarget(this);
		players.Clear();
	}

	private void OnTransform(int sender, Mat4 transform)
	{
		if(players.ContainsKey(sender))
		players[sender].SetWorldTransform(transform);
	}

	private void OnShot(int sender, Vec3 start, Vec3 direction)
	{
		Visualizer.RenderLine3D(start, start + direction * 1000, vec4.RED, 0.1f);

		Node[] excludes = new Node[players[sender].GetRootNodeDummy().NumChildren];
		for(int i =0; i <  excludes.Length; i++)
		{
			excludes[i] = players[sender].GetRootNodeDummy().GetChild(i);
		}

		var obj = World.GetIntersection(start, start + direction * 1000, 1, excludes);
		Node intersected_root_node = obj?.Parent;

		if(intersected_root_node == main_player.GetRootNodeDummy())
		{
			network_logic.SendEventHP(main_player.Damage());
		}
	}

	private void OnHP(int sender, int hp)
	{
		if (players.ContainsKey(sender))
			players[sender].SetHP(hp);
	}

	private void OnMainPlayerDead()
	{
		network_logic.OnEventTransform -= OnTransform;
		network_logic.OnEventShot -= OnShot;
		network_logic.OnEventHP -= OnHP;

		var player_spectator = new PlayerSpectator();
		var player_persecutor = Game.Player as PlayerPersecutor;

		player_persecutor.Target = null;

		player_spectator.WorldTransform = player_persecutor.WorldTransform;

		Game.Player = player_spectator;

		WindowManager.MainWindow.AddChild(leave_button, Gui.ALIGN_CENTER);
	}

	private void OnLeaveRoom()
	{
		foreach (var player in players)
			player.Value.Kill();

		players.Clear();

		leaved = true;

		WindowManager.MainWindow.RemoveChild(leave_button);

		network_logic.LeaveRoom();
	}

	public void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
	{
		UnigineApp.GamePlayer game_player = new UnigineApp.GamePlayer();
		players.Add(newPlayer.ActorNumber, game_player);
	}

	public void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
	{
		players[otherPlayer.ActorNumber].Kill();
	}

	public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) {}

	public void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps) {}

	public void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient) {}
}