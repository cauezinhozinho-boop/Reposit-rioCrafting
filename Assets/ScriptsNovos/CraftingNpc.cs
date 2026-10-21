using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public sealed class CraftingNpc : MonoBehaviour
{
    private sealed class Recipe
    {
        public readonly string Name;
        public readonly ItemType Output;
        public readonly int OutputCount;
        public readonly Dictionary<ItemType, int> Ingredients;
        public readonly CraftingRecipeData Data;

        public Recipe(string name, ItemType output, int outputCount, params object[] ingredients)
        {
            Name = name;
            Output = output;
            OutputCount = outputCount;
            Ingredients = new Dictionary<ItemType, int>();
            for (int i = 0; i < ingredients.Length; i += 2)
                Ingredients[(ItemType)ingredients[i]] = (int)ingredients[i + 1];
        }

        public Recipe(CraftingRecipeData data)
        {
            Data = data;
            Name = data.RecipeName;
            Output = data.Output;
            OutputCount = data.OutputCount;
            Ingredients = data.GetRequirements();
        }
    }

    private static readonly Recipe[] Recipes =
    {
        new Recipe("Sopa de cogumelo", ItemType.SopaDeCogumelo, 1,
            ItemType.FungoComum, 2, ItemType.AguaPotavel, 1),
        new Recipe("Carne cozida", ItemType.CarneCozida, 1,
            ItemType.CarneCrua, 1, ItemType.Wood, 1),
        new Recipe("Substancia calmante", ItemType.SubistanciaCalmante, 1,
            ItemType.FungoLunaSonos, 1, ItemType.Frasco, 1),
        new Recipe("Comunicador", ItemType.Comunicador, 1,
            ItemType.Componentes, 2, ItemType.FiosDeCobre, 1, ItemType.Battery, 1),
        new Recipe("Garrafa termica", ItemType.GarrafaTermica, 1,
            ItemType.Vidro, 2, ItemType.Tecido, 1, ItemType.Fio, 1),
        new Recipe("Cilindro de oxigenio", ItemType.CilindroDeOxigenio, 1,
            ItemType.Ferro, 2, ItemType.Componentes, 1, ItemType.FiosDeCobre, 1)
    };

    private static readonly Dictionary<ItemType, string> ItemNames = new Dictionary<ItemType, string>
    {
        { ItemType.FungoComum, "Fungo comum" },
        { ItemType.FungoLunaSonos, "Fungo Luna-Sonos" },
        { ItemType.AguaPotavel, "Agua potavel" },
        { ItemType.CarneCrua, "Carne crua" },
        { ItemType.Wood, "Madeira" },
        { ItemType.Frasco, "Frasco" },
        { ItemType.Componentes, "Componentes" },
        { ItemType.FiosDeCobre, "Fios de cobre" },
        { ItemType.Battery, "Bateria" },
        { ItemType.Vidro, "Vidro" },
        { ItemType.Tecido, "Tecido" },
        { ItemType.Fio, "Fio" },
        { ItemType.Ferro, "Ferro" },
        { ItemType.SopaDeCogumelo, "Sopa de cogumelo" },
        { ItemType.CarneCozida, "Carne cozida" },
        { ItemType.SubistanciaCalmante, "Substancia calmante" },
        { ItemType.Comunicador, "Comunicador" },
        { ItemType.GarrafaTermica, "Garrafa termica" },
        { ItemType.CilindroDeOxigenio, "Cilindro de oxigenio" }
    };

    public static bool IsMenuOpen { get; private set; }
    public static bool EscapeConsumedThisFrame => Time.frameCount == lastEscapeCloseFrame;
    private static int lastEscapeCloseFrame = -1;

    private static CraftingNpc activeNpc;
    private PlayerInventory playerInventory;
    private PlayerMovement playerMovement;
    private GameObject canvasObject;
    private static GameObject craftingUiPrefab;
    private static bool craftingUiPrefabLoaded;
    private GameObject homePanel;
    private GameObject recipesPanel;
    private GameObject recipeDetailsPanel;
    private TextMeshProUGUI messageText;
    private TextMeshProUGUI recipeDetailsText;
    private Button recipeCraftButton;
    private Recipe selectedRecipe;
    private readonly List<Button> recipeButtons = new List<Button>();
    private readonly List<Recipe> activeRecipes = new List<Recipe>();
    private bool playerNearby;
    private bool playerMovementWasEnabled;
    private static Material npcMaterial;
    private const string GrandpaResourcePath = "Models/Grandpa/Grandpa";
    private const string CraftingUiResourcePath = "UI/CraftingNpcUI";
    private const float GrandpaTargetHeight = 1000f;
    private static GameObject grandpaPrefab;
    private static bool grandpaPrefabLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneSpawner()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ScheduleNpc(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ScheduleNpc(scene);
    }

    private static void ScheduleNpc(Scene scene)
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.StartCoroutine(SpawnAfterSceneLoad(scene));
        else
            EnsureNpc(scene);
    }

    private static IEnumerator SpawnAfterSceneLoad(Scene scene)
    {
        yield return null;
        if (SceneManager.GetActiveScene().handle == scene.handle)
            EnsureNpc(scene);
    }

    private static void EnsureNpc(Scene scene)
    {
        if (scene.name != "InHouse")
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || player.GetComponentInParent<PlayerInventory>() == null)
            return;

        CraftingNpc existingNpc = FindAnyObjectByType<CraftingNpc>();
        GameObject npc = existingNpc != null ? existingNpc.gameObject : new GameObject("Cabin Crafting NPC");
        GameObject spawnPoint = GameObject.Find("SpawnPoint");
        Vector3 anchorPosition = spawnPoint != null ? spawnPoint.transform.position : player.transform.position;
        Vector3 right = spawnPoint != null ? spawnPoint.transform.right : player.transform.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.right;
        npc.transform.position = anchorPosition + right.normalized * 1250f;
        Vector3 direction = player.transform.position - npc.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            npc.transform.rotation = Quaternion.LookRotation(direction);

        if (existingNpc != null)
            return;

        Rigidbody body = npc.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        npc.AddComponent<CapsuleCollider>();
        npc.AddComponent<CraftingNpc>();
    }

    private void Awake()
    {
        Renderer scenePreview = GetComponent<MeshRenderer>();
        if (scenePreview != null) scenePreview.enabled = false;

        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
        if (trigger is CapsuleCollider capsule)
        {
            capsule.height = GrandpaTargetHeight;
            capsule.radius = 350f;
            capsule.center = new Vector3(0f, GrandpaTargetHeight * 0.5f, 0f);
        }

        if (transform.childCount == 0)
        {
            if (!CreateGrandpaVisual())
                CreateFallbackVisual();
        }
    }

    private bool CreateGrandpaVisual()
    {
        if (!grandpaPrefabLoaded)
        {
            grandpaPrefab = Resources.Load<GameObject>(GrandpaResourcePath);
            grandpaPrefabLoaded = true;
        }

        if (grandpaPrefab == null)
        {
            Debug.LogWarning("Modelo do NPC nao carregado em Resources/Models/Grandpa/Grandpa.fbx; usando visual substituto.", this);
            return false;
        }

        GameObject model = Instantiate(grandpaPrefab, transform);
        model.name = "GrandpaModel";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Destroy(model);
            Debug.LogWarning("O arquivo Grandpa.fbx nao contem renderers visiveis; usando visual substituto.", this);
            return false;
        }

        Bounds modelBounds = GetBoundsInNpcSpace(model.transform, renderers);
        if (modelBounds.size.y <= 0.001f)
        {
            Destroy(model);
            Debug.LogWarning("Nao foi possivel medir a altura do Grandpa.fbx; usando visual substituto.", this);
            return false;
        }

        float scale = GrandpaTargetHeight / modelBounds.size.y;
        model.transform.localScale = Vector3.one * scale;
        model.transform.localPosition = new Vector3(
            -modelBounds.center.x * scale,
            -modelBounds.min.y * scale,
            -modelBounds.center.z * scale);
        return true;
    }

    private static Bounds GetBoundsInNpcSpace(Transform modelRoot, Renderer[] renderers)
    {
        Bounds combinedBounds = new Bounds();
        bool hasBounds = false;

        foreach (Renderer modelRenderer in renderers)
        {
            Bounds worldBounds = modelRenderer.bounds;
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 worldCorner = new Vector3(
                    (corner & 1) == 0 ? min.x : max.x,
                    (corner & 2) == 0 ? min.y : max.y,
                    (corner & 4) == 0 ? min.z : max.z);
                Vector3 localCorner = modelRoot.InverseTransformPoint(worldCorner);
                if (!hasBounds)
                {
                    combinedBounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(localCorner);
                }
            }
        }

        return combinedBounds;
    }

    private void CreateFallbackVisual()
    {
        CreatePrimitivePart("Body", PrimitiveType.Capsule, new Vector3(0f, 500f, 0f),
            new Vector3(280f, 450f, 230f), new Color(0.32f, 0.24f, 0.17f));
        CreatePrimitivePart("Head", PrimitiveType.Sphere, new Vector3(0f, 1030f, 0f),
            new Vector3(250f, 250f, 250f), new Color(0.76f, 0.59f, 0.42f));
        CreatePrimitivePart("Left Arm", PrimitiveType.Capsule, new Vector3(-330f, 510f, 0f),
            new Vector3(120f, 310f, 120f), new Color(0.32f, 0.24f, 0.17f));
        CreatePrimitivePart("Right Arm", PrimitiveType.Capsule, new Vector3(330f, 510f, 0f),
            new Vector3(120f, 310f, 120f), new Color(0.32f, 0.24f, 0.17f));
    }

    private void CreatePrimitivePart(string partName, PrimitiveType primitive, Vector3 position,
        Vector3 scale, Color color)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = partName;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        Destroy(part.GetComponent<Collider>());

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (npcMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader != null) npcMaterial = new Material(shader);
            }
            if (npcMaterial == null)
                return;

            renderer.sharedMaterial = npcMaterial;
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            if (npcMaterial.HasProperty("_BaseColor")) properties.SetColor("_BaseColor", color);
            else properties.SetColor("_Color", color);
            renderer.SetPropertyBlock(properties);
        }
    }

    private void Update()
    {
        if (PauseManager.IsPaused)
            return;

        if (IsMenuOpen && activeNpc == this && Input.GetKeyDown(KeyCode.Escape))
        {
            lastEscapeCloseFrame = Time.frameCount;
            CloseMenu();
            return;
        }

        if (playerNearby && !IsMenuOpen && Input.GetKeyDown(KeyCode.E))
            OpenMenu();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInventory = other.GetComponentInParent<PlayerInventory>();
        playerMovement = other.GetComponentInParent<PlayerMovement>();
        playerNearby = playerInventory != null;
        SetPrompt(playerNearby);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerNearby = false;
        SetPrompt(false);
        if (IsMenuOpen && activeNpc == this)
            CloseMenu();
    }

    private void OnDestroy()
    {
        if (activeNpc == this)
            CloseMenu();
        if (canvasObject != null)
            Destroy(canvasObject);
    }

    private void SetPrompt(bool visible)
    {
        if (InteractionUI.Instance != null)
        {
            if (visible) InteractionUI.Instance.ShowPrompt();
            else InteractionUI.Instance.HidePrompt();
        }
    }

    private void OpenMenu()
    {
        if (playerInventory == null || IsMenuOpen)
            return;

        activeNpc = this;
        IsMenuOpen = true;
        if (playerMovement != null)
        {
            playerMovementWasEnabled = playerMovement.enabled;
            playerMovement.enabled = false;
        }

        Rigidbody playerBody = playerMovement != null
            ? playerMovement.GetComponent<Rigidbody>()
            : playerInventory.GetComponent<Rigidbody>();
        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
        }

        if (!BuildInterface())
        {
            IsMenuOpen = false;
            activeNpc = null;
            if (playerMovement != null)
                playerMovement.enabled = playerMovementWasEnabled;
            return;
        }

        ShowHome();
        canvasObject.SetActive(true);
        SetPrompt(false);
    }

    private void CloseMenu()
    {
        if (activeNpc != this)
            return;

        IsMenuOpen = false;
        activeNpc = null;
        if (playerMovement != null)
            playerMovement.enabled = playerMovementWasEnabled;
        if (canvasObject != null)
            canvasObject.SetActive(false);
        if (playerNearby)
            SetPrompt(true);
    }

    private bool BuildInterface()
    {
        if (canvasObject != null)
            return true;

        if (!craftingUiPrefabLoaded)
        {
            craftingUiPrefab = Resources.Load<GameObject>(CraftingUiResourcePath);
            craftingUiPrefabLoaded = true;
        }

        if (craftingUiPrefab == null)
        {
            Debug.LogError("Prefab da interface nao encontrado em Assets/Resources/UI/CraftingNpcUI.prefab.", this);
            return false;
        }

        canvasObject = Instantiate(craftingUiPrefab);
        canvasObject.name = "CraftingNpcCanvas";
        canvasObject.SetActive(false);

        Transform panelTransform = canvasObject.transform.Find("Panel");
        if (panelTransform == null)
        {
            Debug.LogError("O prefab CraftingNpcUI precisa ter um objeto chamado Panel.", this);
            Destroy(canvasObject);
            canvasObject = null;
            return false;
        }

        homePanel = panelTransform.Find("Screens/HomePanel")?.gameObject;
        recipesPanel = panelTransform.Find("Screens/RecipesPanel")?.gameObject;
        recipeDetailsPanel = panelTransform.Find("Screens/RecipeDetailsPanel")?.gameObject;
        messageText = panelTransform.Find("Message")?.GetComponent<TextMeshProUGUI>();
        recipeDetailsText = panelTransform.Find("Screens/RecipeDetailsPanel/Ingredients")?.GetComponent<TextMeshProUGUI>();
        recipeCraftButton = panelTransform.Find("Screens/RecipeDetailsPanel/CraftButton")?.GetComponent<Button>();

        if (homePanel == null || recipesPanel == null || recipeDetailsPanel == null ||
            messageText == null || recipeDetailsText == null || recipeCraftButton == null)
        {
            Debug.LogError("O prefab CraftingNpcUI esta incompleto. Confira os nomes dos tres paineis, Message, Ingredients e CraftButton.", this);
            Destroy(canvasObject);
            canvasObject = null;
            return false;
        }

        Button craftButton = panelTransform.Find("Screens/HomePanel/CraftButton")?.GetComponent<Button>();
        Button exitButton = panelTransform.Find("Screens/HomePanel/ExitButton")?.GetComponent<Button>();
        Button recipeBackButton = panelTransform.Find("Screens/RecipesPanel/BackButton")?.GetComponent<Button>();
        Button detailsBackButton = panelTransform.Find("Screens/RecipeDetailsPanel/BackButton")?.GetComponent<Button>();
        if (craftButton == null || exitButton == null || recipeBackButton == null || detailsBackButton == null)
        {
            Debug.LogError("O prefab CraftingNpcUI esta sem um dos botoes de navegacao esperados.", this);
            Destroy(canvasObject);
            canvasObject = null;
            return false;
        }

        craftButton.onClick.AddListener(ShowRecipes);
        exitButton.onClick.AddListener(CloseMenu);
        recipeBackButton.onClick.AddListener(ShowHome);
        detailsBackButton.onClick.AddListener(ShowRecipes);
        recipeCraftButton.onClick.AddListener(CraftSelectedRecipe);

        if (!BuildRecipeList(panelTransform))
        {
            Debug.LogError("O prefab CraftingNpcUI precisa ter RecipeButton_0 como modelo de receita.", this);
            Destroy(canvasObject);
            canvasObject = null;
            return false;
        }
        for (int i = 0; i < recipeButtons.Count; i++)
        {
            int recipeIndex = i;
            recipeButtons[i].onClick.AddListener(() => ShowRecipe(activeRecipes[recipeIndex]));
        }

        return true;
    }

    private void ShowHome()
    {
        SetVisiblePanel(homePanel);
        messageText.text = "O que voce gostaria de fazer?";
    }

    private void ShowRecipes()
    {
        SetVisiblePanel(recipesPanel);
        messageText.text = "Escolha uma receita";
        for (int i = 0; i < recipeButtons.Count; i++)
        {
            Recipe recipe = activeRecipes[i];
            TextMeshProUGUI label = recipeButtons[i].transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
                label.text = DescribeRecipe(recipe);
        }
    }

    private void ShowRecipe(Recipe recipe)
    {
        selectedRecipe = recipe;
        SetVisiblePanel(recipeDetailsPanel);
        messageText.text = DescribeRecipe(recipe);
        recipeDetailsText.text = BuildIngredientList(recipe);

        bool available = playerInventory != null && playerInventory.HasItems(recipe.Ingredients);
        recipeCraftButton.interactable = available;
        TextMeshProUGUI craftLabel = recipeCraftButton.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (craftLabel != null)
            craftLabel.text = available ? "Criar" : "Materiais insuficientes";
    }

    private void SetVisiblePanel(GameObject visiblePanel)
    {
        homePanel.SetActive(visiblePanel == homePanel);
        recipesPanel.SetActive(visiblePanel == recipesPanel);
        recipeDetailsPanel.SetActive(visiblePanel == recipeDetailsPanel);
    }

    private void CraftSelectedRecipe()
    {
        if (recipeDetailsPanel == null || !recipeDetailsPanel.activeSelf || selectedRecipe == null)
            return;

        Craft(selectedRecipe);
    }

    private void Craft(Recipe recipe)
    {
        if (!IsMenuOpen || activeNpc != this || recipe == null)
            return;

        bool crafted = playerInventory != null &&
            playerInventory.TryCraftItems(recipe.Ingredients, recipe.Output, recipe.OutputCount);
        ShowRecipe(recipe);
        messageText.text = crafted
            ? "Criado: " + GetItemName(recipe.Output) + " x" + recipe.OutputCount
            : "Materiais insuficientes.";
    }

    private string DescribeRecipe(Recipe recipe)
    {
        return recipe.Name + " — " + GetItemName(recipe.Output) + " x" + recipe.OutputCount;
    }

    private string BuildIngredientList(Recipe recipe)
    {
        StringBuilder builder = new StringBuilder("Materiais necessarios:\n\n");
        foreach (KeyValuePair<ItemType, int> ingredient in recipe.Ingredients)
        {
            int owned = playerInventory != null ? playerInventory.GetItemCount(ingredient.Key) : 0;
            builder.Append(GetItemName(ingredient.Key)).Append(": ")
                .Append(owned).Append(" / ").Append(ingredient.Value).Append('\n');
        }
        return builder.ToString();
    }

    private static string GetItemName(ItemType type)
    {
        return ItemNames.TryGetValue(type, out string name) ? name : type.ToString();
    }

    private bool BuildRecipeList(Transform panelTransform)
    {
        activeRecipes.Clear();
        CraftingRecipeCatalog catalog = Resources.Load<CraftingRecipeCatalog>("CraftingRecipeCatalog");
        if (catalog != null && catalog.Recipes != null && catalog.Recipes.Count > 0)
        {
            foreach (CraftingRecipeData data in catalog.Recipes)
                if (data != null) activeRecipes.Add(new Recipe(data));
        }
        else
            activeRecipes.AddRange(Recipes);

        recipeButtons.Clear();
        Transform recipesTransform = panelTransform.Find("Screens/RecipesPanel");
        Transform viewport = recipesTransform.Find("RecipeViewport");
        Transform content = viewport != null ? viewport.Find("RecipeContent") : null;
        Transform templateTransform = content != null ? content.Find("RecipeButton_0") : null;
        Button template = templateTransform != null ? templateTransform.GetComponent<Button>() : null;
        if (template == null || content == null || recipesTransform.GetComponent<ScrollRect>() == null) return false;

        template.transform.SetParent(content, false);
        template.gameObject.SetActive(false);
        for (int i = 0; i < activeRecipes.Count; i++)
        {
            Button button = i == 0 ? template : Instantiate(template, content);
            button.name = "RecipeButton_" + i;
            button.gameObject.SetActive(true);
            recipeButtons.Add(button);
        }
        return true;
    }
}
