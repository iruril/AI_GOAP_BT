using UnityEngine;


[CreateAssetMenu(fileName = "CharacterMeshData", menuName = "Scriptable Object/CharacterMeshData", order = int.MaxValue)]
public class CharacterMeshData : ScriptableObject
{
    [SerializeField] private Mesh characterMesh;
    [SerializeField] private Material[] characterMaterials;
    [SerializeField] private GameObject corpseObject;

    public Mesh CharacterMesh { get => characterMesh; }
    public Material[] CharacterMaterials { get => characterMaterials; }
    public GameObject CorpseObject { get => corpseObject ;}
}
