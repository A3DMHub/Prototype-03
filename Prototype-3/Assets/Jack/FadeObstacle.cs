using UnityEngine;

public class FadeObstacle : MonoBehaviour
{

    [SerializeField] private float visibleValue = 1f;
    [SerializeField] private float hiddenValue = 0.2f;
    [SerializeField] private float fadeSpeed = 5f;

    private Material material;
    private float targetValue;

    private void Awake()
    {
        Renderer rend = GetComponent<Renderer>();

        // Creates an instance of the material so you don't affect every object
        material = rend.material;

        targetValue = visibleValue;
    }

    private void Update()
    {
        float currentValue = material.GetFloat("_AlphaVar");
        float newValue = Mathf.MoveTowards(currentValue, targetValue, fadeSpeed * Time.deltaTime);

        material.SetFloat("_AlphaVar", newValue);
    }

    public void FadeIn()
    {
        targetValue = visibleValue;
    }
    public void FadeOut()
    {
        targetValue = hiddenValue;
    }
}
