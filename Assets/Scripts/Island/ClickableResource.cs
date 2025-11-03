using UnityEngine;

public class ClickableResource : MonoBehaviour
{
    ResourceNode node;

    void Awake()
    {
        node = GetComponent<ResourceNode>();
    }

    void OnMouseDown()
    {
        if (node == null) return;

        // Only act if not in expansion mode
        if (ExpansionModeManager.I != null && ExpansionModeManager.I.IsActive) return;

        if (node.type == ResourceNode.ResourceType.Tree && ToolManager.I.currentTool == ToolManager.Tool.Axe)
        {
            node.Hit();
        }
        else if (node.type == ResourceNode.ResourceType.Rock && ToolManager.I.currentTool == ToolManager.Tool.Pickaxe)
        {
            node.Hit();
        }
        else
        {
            Debug.Log("Wrong tool!");
        }
    }
}
