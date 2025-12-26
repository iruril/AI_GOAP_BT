using UnityEngine;

public class CharacterMeshUpdater : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer characterMeshRenderer;
    [SerializeField] private Transform characterParent;
    [SerializeField] private CorpseGenerator corpseGenerator;

    public void UpdateCharacterMesh(string characterMeshID)
    {
        CharacterMeshData data = Resources.Load<CharacterMeshData>($"ModelData/{characterMeshID}");
        if (data == null)
        {
            Debug.LogError($"CharacterMeshData with ID {characterMeshID} not found!");
            return;
        }

        characterMeshRenderer.sharedMesh = data.CharacterMesh;
        characterMeshRenderer.materials = data.CharacterMaterials;

        GameObject corpseObj = Instantiate(data.CorpseObject, characterParent);
        corpseObj.SetActive(false);
        corpseGenerator.SetCorpseObject(corpseObj);
    }
}
