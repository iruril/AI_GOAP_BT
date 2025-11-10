using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class MousePicker : MonoBehaviour
{
    public Camera Cam;
    public LayerMask GroundMask;
    public List<AINavigator> Navigators = new();

    void Awake()
    {

    }

    public void OnClick(InputAction.CallbackContext ctx)
    {
        Ray ray = Cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, GroundMask))
        {
            foreach (var agent in Navigators)
            {
                agent.SetDestination(AstarPath.active.GetNearest(hit.point).position);
            }
        }
    }
}