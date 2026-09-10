using UnityEngine;

public class BetScreen : MonoBehaviour, IInteractable
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void OnHovered()
    {
        Debug.Log("Mirando a la terminal de apuestas. Texto: [Pulsa E para apostar]");
    }

    public void OnInteract()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
