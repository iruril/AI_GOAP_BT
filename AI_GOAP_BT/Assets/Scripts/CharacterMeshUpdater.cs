using UnityEngine;

public class CharacterMeshUpdater : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer characterMeshRenderer;
    [SerializeField] private Transform characterParent;
    [SerializeField] private CorpseGenerator corpseGenerator;
    private GameObject corpse;

    public void UpdateCharacterMesh(string characterMeshID)
    {
        CharacterMeshData data = Resources.Load<CharacterMeshData>($"ModelData/{characterMeshID}");
        if (data == null)
        {
            Debug.LogError($"CharacterMeshData with ID {characterMeshID} not found!");
            return;
        }

        var anim = GetComponent<Animator>();
        bool wasEnabled = anim.enabled;

        anim.enabled = false;

        characterMeshRenderer.sharedMesh = data.CharacterMesh;
        characterMeshRenderer.materials = data.CharacterMaterials;

        anim.enabled = wasEnabled;

        if (corpse != null) Destroy(corpse);
        corpse = Instantiate(data.CorpseObject, characterParent);
        corpse.SetActive(false);
        corpseGenerator.SetCorpseObject(corpse);
    }
}
