using UnityEngine;

/// <summary>
/// Anexado a qualquer GameObject 3D no cen�rio que o jogador possa pegar.
/// Utiliza o sistema de gatilhos 3D e se integra com a InteractionUI existente.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PickableItem : MonoBehaviour
{
    [Header("Identifica��o do Item")]
    [Tooltip("Define qual � a categoria/fun��o deste item.")]
    public ItemType itemType;

    [Tooltip("Nome amig�vel que aparecer� na mensagem da tela.")]
    public string itemName = "Item";

    [Tooltip("ID �nico caso seja uma chave.")]
    public int itemID = 1;

    [Header("Efeito de Restaura��o")]
    [Tooltip("Quantidade de status que este item recupera.")]
    public float restoreAmount = 50f;

    [Min(1)] public int quantity = 1;
    private bool playerInRange = false;
    private PlayerInventory currentInventory;

    void Update()
    {

        if (PauseManager.IsPaused || CraftingNpc.IsMenuOpen)
            return;

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Collect();
        }
    }

    private void Collect()
    {
        if (currentInventory != null)
        {
            float amount = itemType == ItemType.MedicalKit || itemType == ItemType.WarmDrink || itemType == ItemType.OxygenTank
                ? restoreAmount
                : quantity;
            currentInventory.AddItem(itemType, itemID, amount);

            if (InteractionUI.Instance != null)
            {
                InteractionUI.Instance.HidePrompt();
            }

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            currentInventory = other.GetComponentInParent<PlayerInventory>();

            if (InteractionUI.Instance != null)
            {
                InteractionUI.Instance.ShowPrompt();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            currentInventory = null;

            if (InteractionUI.Instance != null)
            {
                InteractionUI.Instance.HidePrompt();
            }
        }
    }
}
