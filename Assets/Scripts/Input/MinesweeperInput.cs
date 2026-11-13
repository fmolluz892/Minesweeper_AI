using UnityEngine;
using UnityEngine.InputSystem;

public class MinesweeperInput : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;


    // Left Button
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            MinesweeperCell cell = hit.collider.GetComponent <MinesweeperCell>();

            if (cell != null) 
            {
                cell.Interact();
            }
        } 
    }

    // Right Button
    public void OnFlag(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        Debug.Log("Se ha detectado el botón derecho del ratón.");


        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            MinesweeperCell cell = hit.collider.GetComponent<MinesweeperCell>();

            if (cell != null)
            {
                cell.Flag();
            }
        }

    }



}
