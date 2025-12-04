using UnityEngine;

public class PlayerSetup : MonoBehaviour
{
    private CharactersData characterData = ReferenceManager.Instance.CharacterData;

    private SpriteRenderer characterSpriteRenderer;

    private void Awake()
    {
        characterSpriteRenderer = GetComponent<SpriteRenderer>();
    }
    private void Start()
    {
        InitializeCharacter();
    }   

    private void InitializeCharacter()
    {
        if (characterData != null && characterSpriteRenderer != null)
        {
            characterSpriteRenderer.sprite = characterData.characterSprite[0];
        }
        else
        {
            Debug.LogError("[PlayerSetup] CharacterData hoặc CharacterSpriteRenderer chưa được gán đúng.");
        }

    }
}
