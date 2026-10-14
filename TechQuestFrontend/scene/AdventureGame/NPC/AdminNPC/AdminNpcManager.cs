using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public abstract partial class AdminNpcManager : Control
{
	protected abstract string Difficulty { get; }

	private GridContainer itemContainer;
	private Control itemTemplate;
	private Control addPopup;
	private LineEdit npcNameInput;
	private Button imageButton;
	private Button saveButton;
	private Label formMessage;
	private AcceptDialog messageDialog;
	private ConfirmationDialog deleteDialog;
	private ConfirmationDialog deployDialog;
	private FileDialog fileDialog;
	private string selectedImagePath = "";
	private NpcDto editingNpc;
	private NpcDto selectedNpc;
	private readonly List<NpcDto> currentNpcs = new();
	private string npcPanelPath;

	public override void _Ready()
	{
		Hide();
		npcPanelPath = Difficulty + "NPCPanel";
		itemContainer = GetNode<GridContainer>(npcPanelPath + "/NPCListPanel/ScrollContainer/ItemContainer");
		itemTemplate = GetNode<Control>(npcPanelPath + "/NPCListPanel/NPCItemPanel");
		itemTemplate.Hide();
		addPopup = GetNode<Control>(npcPanelPath + "/AddNPCPopup");
		addPopup.Hide();
		npcNameInput = GetNode<LineEdit>(npcPanelPath + "/AddNPCPopup/AddItemPopup/MarginContainer/VBoxContainer/ItemName");
		imageButton = GetNode<Button>(npcPanelPath + "/AddNPCPopup/AddItemPopup/MarginContainer/VBoxContainer/ImageButton");
		saveButton = GetNode<Button>(npcPanelPath + "/AddNPCPopup/AddItemPopup/MarginContainer/VBoxContainer/Buttons/AddButton");
		formMessage = GetNode<Label>(npcPanelPath + "/AddNPCPopup/AddItemPopup/MarginContainer/VBoxContainer/Title");
		messageDialog = GetNode<AcceptDialog>(npcPanelPath + "/AcceptDialog");
		deleteDialog = GetNode<ConfirmationDialog>(npcPanelPath + "/DeleteConfirmationDialog");
		deployDialog = GetNode<ConfirmationDialog>(npcPanelPath + "/DeployConfirmationDialog");

		GetNode<Button>(npcPanelPath + "/AddButton").Pressed += OpenAddForm;
		GetNode<Button>(npcPanelPath + "/CloseButton").Pressed += CloseSetup;
		GetNode<Button>(npcPanelPath + "/AddNPCPopup/AddItemPopup/MarginContainer/VBoxContainer/Buttons/CancelButton").Pressed += CloseForm;
		imageButton.Pressed += OpenImagePicker;
		saveButton.Pressed += SaveNpc;
		deleteDialog.Confirmed += DeleteSelectedNpc;
		deployDialog.Confirmed += DeploySelectedNpc;

		fileDialog = new FileDialog
		{
			Title = "Choose NPC Image",
			FileMode = FileDialog.FileModeEnum.OpenFile,
			Access = FileDialog.AccessEnum.Filesystem,
			UseNativeDialog = true,
			Filters = new string[] { "*.png ; PNG Images", "*.jpg ; JPG Images", "*.jpeg ; JPEG Images" }
		};
		AddChild(fileDialog);
		fileDialog.FileSelected += OnImageSelected;
	}

	public void OpenNpcSetup()
	{
		if (!UserSession.IsAdmin)
			return;

		Show();
		GetNode<Control>(npcPanelPath).Show();
		addPopup.Hide();
		LoadNpcs();
	}

	private void LoadNpcs()
	{
		try
		{
			currentNpcs.Clear();
			currentNpcs.AddRange(NpcApiClient.GetNpcs(Difficulty));
			foreach (Node child in itemContainer.GetChildren())
				child.QueueFree();

			foreach (NpcDto npc in currentNpcs)
				AddNpcCard(npc);
		}
		catch (Exception exception)
		{
			ShowMessage("NPC list could not be loaded: " + exception.Message);
		}
	}

	private void AddNpcCard(NpcDto npc)
	{
		var card = new PanelContainer
		{
			CustomMinimumSize = new Vector2(190, 300),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin
		};
		var layout = new VBoxContainer();
		layout.AddThemeConstantOverride("separation", 8);
		card.AddChild(layout);

		var image = new TextureRect
		{
			CustomMinimumSize = new Vector2(165, 185),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
		};
		layout.AddChild(image);
		var label = new Label
		{
			Text = npc.NpcName,
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		layout.AddChild(label);

		var deployButton = new Button { Text = "Deploy" };
		layout.AddChild(deployButton);
		var editDeleteRow = new HBoxContainer();
		layout.AddChild(editDeleteRow);
		var editButton = new Button { Text = "Edit", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		var deleteButton = new Button { Text = "Delete", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		editDeleteRow.AddChild(editButton);
		editDeleteRow.AddChild(deleteButton);

		deployButton.Pressed += () => ConfirmDeploy(npc);
		editButton.Pressed += () => OpenEditForm(npc);
		deleteButton.Pressed += () => ConfirmDelete(npc);
		itemContainer.AddChild(card);

		if (!string.IsNullOrWhiteSpace(npc.NpcImage))
		{
			try
			{
				image.Texture = AccountApiClient.LoadAvatarAsync(npc.NpcImage).GetAwaiter().GetResult();
			}
			catch (Exception exception)
			{
				GD.PrintErr("NPC image could not be loaded: " + exception.Message);
			}
		}
	}

	private void OpenAddForm()
	{
		editingNpc = null;
		selectedImagePath = "";
		npcNameInput.Text = "";
		imageButton.Text = "SELECT IMAGE (.png / .jpg / .jpeg)";
		saveButton.Text = "SAVE";
		formMessage.Text = "Add NPC";
		addPopup.Show();
	}

	private void OpenEditForm(NpcDto npc)
	{
		editingNpc = npc;
		selectedImagePath = "";
		npcNameInput.Text = npc.NpcName;
		imageButton.Text = "SELECT NEW IMAGE (OPTIONAL)";
		saveButton.Text = "SAVE";
		formMessage.Text = "Edit NPC";
		addPopup.Show();
	}

	private void OpenImagePicker()
	{
		fileDialog.PopupCentered(new Vector2I(720, 520));
	}

	private void OnImageSelected(string path)
	{
		string extension = Path.GetExtension(path).ToLowerInvariant();
		if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
		{
			ShowMessage("Image must be PNG, JPG, or JPEG.");
			return;
		}

		selectedImagePath = path;
		imageButton.Text = Path.GetFileName(path);
	}

	private void SaveNpc()
	{
		string name = npcNameInput.Text.Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			ShowMessage("NPC name is required.");
			return;
		}
		if (editingNpc == null && string.IsNullOrWhiteSpace(selectedImagePath))
		{
			ShowMessage("NPC image is required.");
			return;
		}

		try
		{
			string imagePath = editingNpc?.NpcImage ?? "";
			if (!string.IsNullOrWhiteSpace(selectedImagePath))
				imagePath = NpcApiClient.UploadImage(selectedImagePath);

			var npc = new NpcDto
			{
				NpcId = editingNpc?.NpcId ?? 0,
				NpcName = name,
				NpcImage = imagePath,
				Difficulty = Difficulty,
				Status = "active"
			};
			NpcApiClient.SaveNpc(npc);
			addPopup.Hide();
			LoadNpcs();
			ShowMessage("NPC saved successfully.");
		}
		catch (Exception exception)
		{
			ShowMessage("NPC could not be saved: " + exception.Message);
		}
	}

	private void ConfirmDelete(NpcDto npc)
	{
		selectedNpc = npc;
		deleteDialog.DialogText = $"Delete {npc.NpcName}?";
		deleteDialog.PopupCentered();
	}

	private void DeleteSelectedNpc()
	{
		if (selectedNpc == null)
			return;

		try
		{
			NpcApiClient.DeleteNpc(selectedNpc.NpcId);
			RemoveNpcSprites(selectedNpc.NpcId);
			selectedNpc = null;
			LoadNpcs();
			ShowMessage("NPC deleted successfully.");
		}
		catch (Exception exception)
		{
			ShowMessage("NPC could not be deleted: " + exception.Message);
		}
	}

	private void ConfirmDeploy(NpcDto npc)
	{
		selectedNpc = npc;
		deployDialog.DialogText = $"Deploy {npc.NpcName} to the {Difficulty} map?";
		deployDialog.PopupCentered();
	}

	private void DeploySelectedNpc()
	{
		if (selectedNpc == null)
			return;

		try
		{
			Node currentScene = GetTree().CurrentScene;
			Node spawnPoints = currentScene?.GetNodeOrNull<Node>("NPCSpawnPoints");
			if (spawnPoints == null)
				throw new InvalidOperationException("This map has no NPCSpawnPoints node.");

			var markers = new List<Marker2D>();
			foreach (Node child in spawnPoints.GetChildren())
			{
				if (child is Marker2D marker)
					markers.Add(marker);
			}
			if (markers.Count == 0)
				throw new InvalidOperationException("This map has no NPC spawn markers.");

			Marker2D markerChoice = markers[GD.RandRange(0, markers.Count - 1)];
			Vector2 position = markerChoice.GlobalPosition;
			Texture2D texture = AccountApiClient.LoadAvatarAsync(selectedNpc.NpcImage).GetAwaiter().GetResult();
			if (texture == null)
				throw new InvalidOperationException("NPC image could not be loaded.");

			NpcApiClient.DeployNpc(selectedNpc, Difficulty, markerChoice.Name, position);
			SpawnNpcSprite(currentScene, selectedNpc, position, texture);
			ShowMessage("NPC deployed successfully.");
		}
		catch (Exception exception)
		{
			ShowMessage("NPC deployment failed: " + exception.Message);
		}
	}

	private void SpawnNpcSprite(Node currentScene, NpcDto npc, Vector2 position, Texture2D texture)
	{
		AnimatedSprite2D sprite = NpcMapSprite.CreateIdleSprite(texture);
		currentScene.AddChild(sprite);
		sprite.GlobalPosition = position;
		sprite.SetMeta("NpcId", npc.NpcId);
	}

	private void RemoveNpcSprites(int npcId)
	{
		Node currentScene = GetTree().CurrentScene;
		if (currentScene == null)
			return;

		foreach (Node child in currentScene.GetChildren())
		{
			if (child is AnimatedSprite2D sprite &&
				sprite.HasMeta("NpcId") &&
				sprite.GetMeta("NpcId").AsInt32() == npcId)
				sprite.QueueFree();
		}
	}

	private void CloseForm()
	{
		addPopup.Hide();
		editingNpc = null;
		selectedImagePath = "";
	}

	private void CloseSetup()
	{
		addPopup.Hide();
		Hide();
	}

	private void ShowMessage(string text)
	{
		messageDialog.DialogText = text;
		messageDialog.PopupCentered();
	}
}
