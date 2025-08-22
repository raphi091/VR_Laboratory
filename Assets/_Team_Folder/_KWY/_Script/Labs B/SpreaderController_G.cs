using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpreaderController_G : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("도말을 완료하는 데 필요한 시간(초)")]
    public float spreadDuration = 2.0f;

    [Header("멸균 설정")]
    [Tooltip("멸균하는 데 필요한 시간(초)")]
    public float sterilizationDuration = 2.0f;

    [Tooltip("멸균되었을 때 색상")]
    public Color sterilizedColor = Color.red;

    [Tooltip("색상이 변하는 데 걸리는 시간(초)")]
    public float colorChangeDuration = 1.0f;

    [Tooltip("Spreader Mesh Renderer")]
    public MeshRenderer spreaderRenderer;

    private float spreadingTimeElapsed = 0f;
    private PetriDishController_G currentDish;
    private Color originalColor;
    private bool isSterilized = false;
    private Coroutine runningColorAnimation;


    private void Start()
    {
        if (spreaderRenderer != null)
        {

            originalColor = spreaderRenderer.material.GetColor("_Color");

        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PetriDishController_G dish = other.GetComponent<PetriDishController_G>();
        if (dish != null)
        {
            currentDish = dish;
        }

        AlcoholLampController_G lamp = other.GetComponentInParent<AlcoholLampController_G>();
        if (lamp != null && lamp.isLit && !isSterilized)
        {
            if (runningColorAnimation != null) 
                StopCoroutine(runningColorAnimation);

            runningColorAnimation = StartCoroutine(SterilizeRoutine());
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isSterilized && currentDish != null && other.gameObject.GetComponent<PetriDishController_G>() == currentDish && currentDish.currentState == PetriDishController_G.DishState.Inoculated)
        {
            spreadingTimeElapsed += Time.deltaTime;

            if (spreadingTimeElapsed >= spreadDuration)
            {
                currentDish.CompleteSpreading();
                spreadingTimeElapsed = 0f;
                currentDish = null;
                isSterilized = false;

                if (runningColorAnimation != null) 
                    StopCoroutine(runningColorAnimation);

                runningColorAnimation = StartCoroutine(CooldownRoutine());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PetriDishController_G dish = other.GetComponent<PetriDishController_G>();
        if (dish == currentDish)
        {
            currentDish = null;
            spreadingTimeElapsed = 0f;
        }

        if (other.GetComponentInParent<AlcoholLampController_G>() != null)
        {
            if (runningColorAnimation != null) 
                StopCoroutine(runningColorAnimation);
        }
    }

    private IEnumerator SterilizeRoutine()
    {
        Debug.Log("Spreader 멸균 시작...");
        float elapsedTime = 0f;
        Color startColor = spreaderRenderer.material.GetColor("_Color");
        spreaderRenderer.material.EnableKeyword("_COLOR");

        while (elapsedTime < colorChangeDuration)
        {
            elapsedTime += Time.deltaTime;
            Color newColor = Color.Lerp(startColor, sterilizedColor, elapsedTime / colorChangeDuration);
            spreaderRenderer.material.SetColor("_Color", newColor);
            
            yield return null;
        }

        spreaderRenderer.material.SetColor("_Color", sterilizedColor);

        yield return new WaitForSeconds(sterilizationDuration);

        isSterilized = true;
        Debug.Log("Spreader 멸균 완료!");
    }

    private IEnumerator CooldownRoutine()
    {
        float elapsedTime = 0f;
        Color startColor = spreaderRenderer.material.GetColor("_Color");

        while (elapsedTime < colorChangeDuration)
        {
            elapsedTime += Time.deltaTime;
            Color newColor = Color.Lerp(startColor, originalColor, elapsedTime / colorChangeDuration);
            spreaderRenderer.material.SetColor("_Color", newColor);
            yield return null;
        }

        spreaderRenderer.material.SetColor("_Color", originalColor);
    }
}
