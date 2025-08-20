using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpreaderController_G : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("도말을 완료하는 데 필요한 시간(초)")]
    public float spreadDuration = 2.0f;

    private float spreadingTimeElapsed = 0f;
    private PetriDishController_G currentDish;


    private void OnTriggerEnter(Collider other)
    {
        PetriDishController_G dish = other.GetComponent<PetriDishController_G>();

        if (dish != null)
        {
            currentDish = dish;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (currentDish != null && other.gameObject.GetComponent<PetriDishController_G>() == currentDish && currentDish.currentState == PetriDishController_G.DishState.Inoculated)
        {
            spreadingTimeElapsed += Time.deltaTime;

            if (spreadingTimeElapsed >= spreadDuration)
            {
                currentDish.CompleteSpreading();

                spreadingTimeElapsed = 0f;
                currentDish = null;
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
    }
}
