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

    [Header("아웃라인 설정")]
    [Tooltip("멸균되었을 때 표시할 아웃라인 색상")]
    public Color sterilizedOutlineColor = Color.red;

    [Tooltip("Spreader Outline")]
    public Outline spreaderOutline;

    private float spreadingTimeElapsed = 0f;
    private PetriDishController_G currentDish;
    private Color originalOutlineColor;
    private bool isSterilized = false;
    private Coroutine runningColorAnimation;


    private void Start()
    {
        if (spreaderOutline != null)
        {
            originalOutlineColor = spreaderOutline.OutlineColor;
            spreaderOutline.enabled = false;
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
            StartCoroutine(SterilizeRoutine());
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

                if (spreaderOutline != null)
                {
                    spreaderOutline.enabled = false;
                    spreaderOutline.OutlineColor = originalOutlineColor;
                }
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
            StopAllCoroutines();
        }
    }

    private IEnumerator SterilizeRoutine()
    {
        Debug.Log("Spreader 멸균 시작...");
      
        yield return new WaitForSeconds(sterilizationDuration);

        isSterilized = true;
        Debug.Log("Spreader 멸균 완료!");

        if (spreaderOutline != null)
        {
            spreaderOutline.OutlineColor = sterilizedOutlineColor;
            spreaderOutline.enabled = true;
        }
    }
}
