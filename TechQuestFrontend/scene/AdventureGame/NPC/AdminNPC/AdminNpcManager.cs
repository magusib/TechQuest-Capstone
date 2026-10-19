using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public abstract partial class AdminNpcManager : Control
{
	protected abstract string Difficulty { get; }

	private GridContainer itemContainer;
	private Control itemTemplate;
	private Control addPopup;
	private LineEdit npcNameInput;
	private Button imageButton;
	private Button animationSceneButton;
	private Button clearAnimationSceneButton;
	private Label animationSceneLabel;
	private Button saveButton;
	private Label formMessage;
	private AcceptDialog messageDialog;
	private ConfirmationDialog deleteDialog;
	private ConfirmationDialog deployDialog;
	private ConfirmationDialog undeployDialog;
	private FileDialog fileDialog;
	private FileDialog animationSceneDialog;
	private string selectedImagePath = "";
	private string selectedAnimationScenePath = "";
	private NpcDto editingNpc;
	private NpcDto selectedNpc;
	private NpcDeploymentDto selectedDeployment;
	private readonly List<NpcDto> currentNpcs = new();
	private readonly List<NpcDeploymentDto> currentDeployments = new();
	private string npcPanelPath;
	private bool showingDeployedNpcs;
	private bool isLoadingNpcs;
	private bool isSavingNpc;
	private bool isDeletingNpc;
	private bool isDeployingNpc;
	private bool isUndeployingNpc;
	private bool isSavingConfiguration;
	private Control configurationPopup;
	private LineEdit subjectInput;
	private Label configurationTitle;
	private OptionButton miniGameInput;
	private TextEdit questionInput;
	private LineEdit correctAnswerInput;
	private TextEdit answerTipInput;
	private Button saveConfigurationButton;
	private NpcConfigurationDto editingConfiguration;

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
		AddAnimationScenePicker();
		messageDialog = GetNode<AcceptDialog>(npcPanelPath + "/AcceptDialog");
		deleteDialog = GetNode<ConfirmationDialog>(npcPanelPath + "/DeleteConfirmationDialog");
		deployDialog = GetNode<ConfirmationDialog>(npcPanelPath + "/DeployConfirmationDialog");
		undeployDialog = new ConfirmationDialog
		{
			Title = "Undeploy NPC",
			OkButtonText = "Undeploy",
			DialogText = "Undeploy this NPC from the map?"
		};
		AddChild(undeployDialog);
		CreateConfigurationPopup();

		GetNode<Button>(npcPanelPath + "/AddButton").Pressed += OpenAddForm;
		GetNode<Button>(npcPanelPath + "/CloseButton").Pressed += CloseSetup;
		GetNode<Button>(npcPanelPath + "/AddNPCPopup/AddItemPopup/MarginContainer/VBoxContainer/Buttons/CancelButton").Pressed += CloseForm;
		imageButton.Pressed += OpenImagePicker;
		animationSceneButton.Pressed += OpenAnimationScenePicker;
		clearAnimationSceneButton.Pressed += ClearAnimationScene;
		saveButton.Pressed += SaveNpc;
		deleteDialog.Confirmed += DeleteSelectedNpc;
		deployDialog.Confirmed += DeploySelectedNpc;
		undeployDialog.Confirmed += UndeploySelectedNpc;

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

		animationSceneDialog = new FileDialog
		{
			Title = "Choose NPC Animation Scene",
			FileMode = FileDialog.FileModeEnum.OpenFile,
			Access = FileDialog.AccessEnum.Resources,
			Filters = new string[] { "*.tscn ; Godot Scenes" }
		};
		AddChild(animationSceneDialog);
		animationSceneDialog.FileSelected += OnAnimationSceneSelected;
	}

	private void AddAnimationScenePicker()
	{
		var fields = (VBoxContainer)imageButton.GetParent();
		int insertIndex = imageButton.GetIndex() + 1;

		animationSceneLabel = new Label
		{
			Text = "Animation: Uploaded image (static)",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		fields.AddChild(animationSceneLabel);
		fields.MoveChild(animationSceneLabel, insertIndex);

		var buttons = new HBoxContainer();
		animationSceneButton = new Button
		{
			Text = "Select Animation Scene",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		clearAnimationSceneButton = new Button { Text = "Clear" };
		buttons.AddChild(animationSceneButton);
		buttons.AddChild(clearAnimationSceneButton);
		fields.AddChild(buttons);
		fields.MoveChild(buttons, insertIndex + 1);
	}

	public void OpenNpcSetup()
	{
		OpenUndeployedNpcList();
	}

	public void OpenDeployedNpcList()
	{
		OpenNpcList(true);
	}

	public void OpenUndeployedNpcList()
	{
		OpenNpcList(false);
	}

	private async void OpenNpcList(bool showDeployed)
	{
		if (!UserSession.IsAdmin)
			return;

		showingDeployedNpcs = showDeployed;
		Show();
		GetNode<Control>(npcPanelPath).Show();
		addPopup.Hide();
		configurationPopup.Hide();
		GetNode<Label>(npcPanelPath + "/" + Difficulty + "NPCLabel").Text = showDeployed
			? "Manage Deployed NPCs"
			: "NPC Inventory";
		GetNode<Button>(npcPanelPath + "/AddButton").Visible = !showDeployed;
		await LoadNpcsAsync();
	}

	private async Task LoadNpcsAsync()
	{
		if (isLoadingNpcs)
			return;

		isLoadingNpcs = true;
		try
		{
			currentNpcs.Clear();
			currentNpcs.AddRange(await NpcApiClient.GetNpcsAsync(Difficulty));
			currentDeployments.Clear();
			currentDeployments.AddRange(await NpcApiClient.GetDeploymentsAsync(Difficulty));
			foreach (Node child in itemContainer.GetChildren())
				child.QueueFree();

			if (!showingDeployedNpcs)
			{
				foreach (NpcDto npc in currentNpcs)
					await AddNpcCardAsync(npc, null);
			}

			if (showingDeployedNpcs)
			{
				foreach (NpcDeploymentDto deployment in currentDeployments)
				{
					NpcDto npc = FindNpc(deployment.NpcId);
					if (npc != null)
						await AddNpcCardAsync(npc, deployment);
				}
			}
		}
		catch (Exception exception)
		{
			ShowMessage("NPC list could not be loaded: " + exception.Message);
		}
		finally
		{
			isLoadingNpcs = false;
		}
	}

	private async Task AddNpcCardAsync(NpcDto npc, NpcDeploymentDto deployment)
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

		if (deployment != null)
		{
			var statusLabel = new Label
			{
				Text = deployment.Status == "Pending"
					? "Pending - Waiting for an available spawn point."
					: "Deployed",
				HorizontalAlignment = HorizontalAlignment.Center,
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			};
			layout.AddChild(statusLabel);
			var actionRow = new HBoxContainer();
			layout.AddChild(actionRow);
			var manageButton = new Button
			{
				Text = "Manage",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			var undeployButton = new Button
			{
				Text = deployment.Status == "Pending" ? "Cancel Pending" : "Undeploy",
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};
			actionRow.AddChild(manageButton);
			actionRow.AddChild(undeployButton);
			manageButton.Pressed += () => OpenConfigurationAsync(npc, deployment);
			undeployButton.Pressed += () => ConfirmUndeploy(npc, deployment);
			if (deployment.Status == "Pending")
				manageButton.Text = "Set Data";
		}
		else
		{
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
		}
		itemContainer.AddChild(card);

		if (!string.IsNullOrWhiteSpace(npc.NpcImage))
		{
			try
			{
				image.Texture = await AccountApiClient.LoadAvatarAsync(npc.NpcImage);
			}
			catch (Exception exception)
			{
				GD.PrintErr("NPC image could not be loaded: " + exception.Message);
			}
		}
	}

	private NpcDto FindNpc(int npcId)
	{
		foreach (NpcDto npc in currentNpcs)
		{
			if (npc.NpcId == npcId)
				return npc;
		}

		return null;
	}

	private void CreateConfigurationPopup()
	{
		configurationPopup = new Control
		{
			Name = "NpcConfigurationPopup",
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Stop,
			ZIndex = 50
		};
		configurationPopup.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(configurationPopup);

		var background = new ColorRect
		{
			Color = new Color(0, 0, 0, 0.65f),
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		configurationPopup.AddChild(background);

		var panel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(560, 620)
		};
		panel.SetAnchorsPreset(Control.LayoutPreset.Center);
		panel.OffsetLeft = -280;
		panel.OffsetTop = -310;
		panel.OffsetRight = 280;
		panel.OffsetBottom = 310;
		configurationPopup.AddChild(panel);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 20);
		margin.AddThemeConstantOverride("margin_top", 18);
		margin.AddThemeConstantOverride("margin_right", 20);
		margin.AddThemeConstantOverride("margin_bottom", 18);
		panel.AddChild(margin);

		var fields = new VBoxContainer();
		fields.AddThemeConstantOverride("separation", 8);
		margin.AddChild(fields);
		configurationTitle = new Label
		{
			Text = "Manage NPC",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		fields.AddChild(configurationTitle);

		subjectInput = AddLineEditField(fields, "Subject Name");
		miniGameInput = new OptionButton();
		AddField(fields, "Mini Game", miniGameInput);
		questionInput = AddTextField(fields, "Question", 90);
		correctAnswerInput = AddLineEditField(fields, "Correct Answer");
		answerTipInput = AddTextField(fields, "Answer Tip", 70);

		var buttons = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.End
		};
		fields.AddChild(buttons);
		var cancelButton = new Button { Text = "Cancel" };
		saveConfigurationButton = new Button { Text = "Save Changes" };
		buttons.AddChild(cancelButton);
		buttons.AddChild(saveConfigurationButton);
		cancelButton.Pressed += () => configurationPopup.Hide();
		saveConfigurationButton.Pressed += SaveConfigurationAsync;
	}

	private static LineEdit AddLineEditField(VBoxContainer fields, string label)
	{
		var input = new LineEdit();
		AddField(fields, label, input);
		return input;
	}

	private static TextEdit AddTextField(VBoxContainer fields, string label, float height)
	{
		var input = new TextEdit
		{
			CustomMinimumSize = new Vector2(0, height)
		};
		AddField(fields, label, input);
		return input;
	}

	private static void AddField(VBoxContainer fields, string label, Control input)
	{
		fields.AddChild(new Label { Text = label });
		fields.AddChild(input);
	}

	private async void OpenConfigurationAsync(NpcDto npc, NpcDeploymentDto deployment)
	{
		try
		{
			NpcConfigurationDto configuration = await NpcApiClient.GetDeploymentConfigurationAsync(
				deployment.DeploymentId);
			List<MiniGameDto> miniGames = await NpcApiClient.GetMiniGamesAsync();
			if (!IsInsideTree())
				return;

			editingConfiguration = configuration;
			configurationTitle.Text = "Manage " + npc.NpcName + " (Deployment " +
				deployment.DeploymentId + ")";
			subjectInput.Text = configuration.SubjectName;
			questionInput.Text = configuration.Question;
			correctAnswerInput.Text = configuration.CorrectAnswer;
			answerTipInput.Text = configuration.AnswerTip;
			miniGameInput.Clear();
			miniGameInput.AddItem("No mini-game", 0);
			foreach (MiniGameDto miniGame in miniGames)
			{
				if (IsSupportedMiniGameType(miniGame.GameType))
					miniGameInput.AddItem(miniGame.GameName, miniGame.MiniGameId);
			}

			miniGameInput.Select(0);
			if (configuration.MiniGameId.HasValue)
			{
				for (int index = 1; index < miniGameInput.ItemCount; index++)
				{
					if (miniGameInput.GetItemId(index) == configuration.MiniGameId.Value)
					{
						miniGameInput.Select(index);
						break;
					}
				}
			}

			configurationPopup.Show();
		}
		catch (Exception exception)
		{
			ShowMessage("NPC information could not be loaded: " + exception.Message);
		}
	}

	private static bool IsSupportedMiniGameType(string gameType)
	{
		return gameType == "MatchThePairs" ||
			gameType == "FallingDuck" ||
			gameType == "QuickTapChallenge";
	}

	private async void SaveConfigurationAsync()
	{
		if (editingConfiguration == null || isSavingConfiguration)
			return;

		if (string.IsNullOrWhiteSpace(subjectInput.Text) ||
			string.IsNullOrWhiteSpace(questionInput.Text) ||
			string.IsNullOrWhiteSpace(correctAnswerInput.Text))
		{
			ShowMessage("Subject name, question, and correct answer are required.");
			return;
		}

		isSavingConfiguration = true;
		saveConfigurationButton.Disabled = true;
		editingConfiguration.SubjectName = subjectInput.Text.Trim();
		int miniGameId = miniGameInput.GetItemId(miniGameInput.Selected);
		editingConfiguration.MiniGameId = miniGameId > 0 ? miniGameId : null;
		editingConfiguration.Question = questionInput.Text.Trim();
		editingConfiguration.CorrectAnswer = correctAnswerInput.Text.Trim();
		editingConfiguration.AnswerTip = answerTipInput.Text.Trim();
		try
		{
			await NpcApiClient.SaveDeploymentConfigurationAsync(
				editingConfiguration.DeploymentId ?? 0,
				editingConfiguration);
			configurationPopup.Hide();
			ShowMessage("NPC information saved successfully.");
		}
		catch (Exception exception)
		{
			ShowMessage("NPC information could not be saved: " + exception.Message);
		}
		finally
		{
			isSavingConfiguration = false;
			saveConfigurationButton.Disabled = false;
		}
	}

	private void ConfirmUndeploy(NpcDto npc, NpcDeploymentDto deployment)
	{
		selectedNpc = npc;
		selectedDeployment = deployment;
		undeployDialog.DialogText = deployment.Status == "Pending"
			? $"Cancel the pending deployment of {npc.NpcName}?"
			: $"Undeploy {npc.NpcName} from the {Difficulty} map?";
		undeployDialog.PopupCentered();
	}

	private async void UndeploySelectedNpc()
	{
		if (selectedNpc == null || selectedDeployment == null || isUndeployingNpc)
			return;

		isUndeployingNpc = true;
		NpcDto npcToUndeploy = selectedNpc;
		NpcDeploymentDto deploymentToRemove = selectedDeployment;
		try
		{
			await NpcApiClient.UndeployNpcAsync(deploymentToRemove.DeploymentId);
			RemoveDeploymentSprite(deploymentToRemove.DeploymentId);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await RefreshMapDeploymentsAsync();
			await LoadNpcsAsync();
			ShowMessage(deploymentToRemove.Status == "Pending"
				? $"Pending deployment for {npcToUndeploy.NpcName} was cancelled."
				: $"{npcToUndeploy.NpcName} was undeployed.");
		}
		catch (Exception exception)
		{
			ShowMessage("NPC could not be undeployed: " + exception.Message);
		}
		finally
		{
			selectedNpc = null;
			selectedDeployment = null;
			isUndeployingNpc = false;
		}
	}

	private void OpenAddForm()
	{
		editingNpc = null;
		selectedImagePath = "";
		selectedAnimationScenePath = "";
		npcNameInput.Text = "";
		imageButton.Text = "SELECT IMAGE (.png / .jpg / .jpeg)";
		UpdateAnimationSceneLabel();
		saveButton.Text = "SAVE";
		formMessage.Text = "Add NPC";
		addPopup.Show();
	}

	private void OpenEditForm(NpcDto npc)
	{
		editingNpc = npc;
		selectedImagePath = "";
		selectedAnimationScenePath = npc.AnimationScenePath ?? "";
		npcNameInput.Text = npc.NpcName;
		imageButton.Text = "SELECT NEW IMAGE (OPTIONAL)";
		UpdateAnimationSceneLabel();
		saveButton.Text = "SAVE";
		formMessage.Text = "Edit NPC";
		addPopup.Show();
	}

	private void OpenImagePicker()
	{
		fileDialog.PopupCentered(new Vector2I(720, 520));
	}

	private void OpenAnimationScenePicker()
	{
		animationSceneDialog.PopupCentered(new Vector2I(900, 600));
	}

	private void OnAnimationSceneSelected(string path)
	{
		string expectedPrefix = $"res://scene/AdventureGame/NPC/{Difficulty}MapNPC/";
		if (!path.StartsWith(expectedPrefix, StringComparison.Ordinal) ||
			!path.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
		{
			ShowMessage($"Choose an animated scene from the {Difficulty} NPC folder.");
			return;
		}

		AnimatedSprite2D previewSprite = null;
		try
		{
			previewSprite = NpcMapSprite.CreateNpcSprite(null, path);
			selectedAnimationScenePath = path;
			UpdateAnimationSceneLabel();
		}
		catch (Exception exception)
		{
			ShowMessage("NPC animation scene could not be used: " + exception.Message);
		}
		finally
		{
			previewSprite?.Free();
		}
	}

	private void ClearAnimationScene()
	{
		selectedAnimationScenePath = "";
		UpdateAnimationSceneLabel();
	}

	private void UpdateAnimationSceneLabel()
	{
		animationSceneLabel.Text = string.IsNullOrWhiteSpace(selectedAnimationScenePath)
			? "Animation: Uploaded image (static)"
			: "Animation: " + Path.GetFileName(selectedAnimationScenePath);
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

	private async void SaveNpc()
	{
		if (isSavingNpc)
			return;

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

		isSavingNpc = true;
		saveButton.Disabled = true;
		try
		{
			string imagePath = editingNpc?.NpcImage ?? "";
			if (!string.IsNullOrWhiteSpace(selectedImagePath))
				imagePath = await NpcApiClient.UploadImageAsync(selectedImagePath);

			var npc = new NpcDto
			{
				NpcId = editingNpc?.NpcId ?? 0,
				NpcName = name,
				NpcImage = imagePath,
				AnimationScenePath = selectedAnimationScenePath,
				Difficulty = Difficulty,
				Status = "active"
			};
			await NpcApiClient.SaveNpcAsync(npc);
			addPopup.Hide();
			await LoadNpcsAsync();
			ShowMessage("NPC saved successfully.");
		}
		catch (Exception exception)
		{
			ShowMessage("NPC could not be saved: " + exception.Message);
		}
		finally
		{
			isSavingNpc = false;
			saveButton.Disabled = false;
		}
	}

	private void ConfirmDelete(NpcDto npc)
	{
		selectedNpc = npc;
		deleteDialog.DialogText = $"Delete {npc.NpcName}?";
		deleteDialog.PopupCentered();
	}

	private async void DeleteSelectedNpc()
	{
		if (selectedNpc == null || isDeletingNpc)
			return;

		isDeletingNpc = true;
		NpcDto npcToDelete = selectedNpc;
		try
		{
			await NpcApiClient.DeleteNpcAsync(npcToDelete.NpcId);
			RemoveNpcSprites(npcToDelete.NpcId);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await RefreshMapDeploymentsAsync();
			selectedNpc = null;
			await LoadNpcsAsync();
			ShowMessage("NPC deleted successfully.");
		}
		catch (Exception exception)
		{
			ShowMessage("NPC could not be deleted: " + exception.Message);
		}
		finally
		{
			isDeletingNpc = false;
		}
	}

	private void ConfirmDeploy(NpcDto npc)
	{
		selectedNpc = npc;
		deployDialog.DialogText = $"Deploy {npc.NpcName} to the {Difficulty} map?";
		deployDialog.PopupCentered();
	}

	private async void DeploySelectedNpc()
	{
		if (selectedNpc == null || isDeployingNpc)
			return;

		isDeployingNpc = true;
		NpcDto npcToDeploy = selectedNpc;
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

			List<NpcDeploymentDto> activeDeployments = await NpcApiClient.GetDeploymentsAsync(Difficulty);
			var occupiedMarkers = new HashSet<string>(StringComparer.Ordinal);
			foreach (NpcDeploymentDto activeDeployment in activeDeployments)
			{
				if (activeDeployment.Status == "Deployed" &&
					!string.IsNullOrWhiteSpace(activeDeployment.PositionMarker))
					occupiedMarkers.Add(activeDeployment.PositionMarker);
			}
			foreach (Node child in currentScene.GetChildren())
			{
				if (child is AnimatedSprite2D sprite &&
					!sprite.IsQueuedForDeletion() &&
					sprite.HasMeta("PositionMarker"))
					occupiedMarkers.Add(sprite.GetMeta("PositionMarker").AsString());
			}

			var availableMarkers = new List<Marker2D>();
			foreach (Marker2D marker in markers)
			{
				if (!occupiedMarkers.Contains(marker.Name.ToString()))
					availableMarkers.Add(marker);
			}

			Marker2D markerChoice = availableMarkers.Count > 0
				? availableMarkers[GD.RandRange(0, availableMarkers.Count - 1)]
				: null;
			Vector2? position = markerChoice == null ? null : markerChoice.GlobalPosition;

			NpcDeploymentDto deployment = await NpcApiClient.DeployNpcAsync(
				npcToDeploy,
				Difficulty,
				markerChoice?.Name,
				position);
			await RefreshMapDeploymentsAsync();
			List<NpcDeploymentDto> updatedDeployments = await NpcApiClient.GetDeploymentsAsync(Difficulty);
			NpcDeploymentDto updatedDeployment = updatedDeployments.Find(
				item => item.DeploymentId == deployment.DeploymentId) ?? deployment;
			await LoadNpcsAsync();
			ShowMessage(updatedDeployment.Status == "Pending"
				? "No available spawn point. The NPC deployment has been saved and will be deployed automatically when a spawn point becomes available."
				: "NPC deployed successfully.");
		}
		catch (Exception exception)
		{
			ShowMessage("NPC deployment failed: " + exception.Message);
		}
		finally
		{
			isDeployingNpc = false;
		}
	}

	private async Task RefreshMapDeploymentsAsync()
	{
		Node currentScene = GetTree().CurrentScene;
		NpcDeploymentRenderer renderer =
			currentScene?.GetNodeOrNull<NpcDeploymentRenderer>("NPCSpawnPoints");
		if (renderer != null)
			await renderer.RefreshDeploymentsAsync();
	}

	private void RemoveDeploymentSprite(int deploymentId)
	{
		Node currentScene = GetTree().CurrentScene;
		if (currentScene == null)
			return;

		foreach (Node child in currentScene.GetChildren())
		{
			if (child is AnimatedSprite2D sprite &&
				sprite.HasMeta("DeploymentId") &&
				sprite.GetMeta("DeploymentId").AsInt32() == deploymentId)
				sprite.QueueFree();
		}
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
		configurationPopup.Hide();
		Hide();
	}

	private void ShowMessage(string text)
	{
		messageDialog.DialogText = text;
		messageDialog.PopupCentered();
	}
}
