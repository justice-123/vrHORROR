using System;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class HintButton : MonoBehaviour
{

    public static HintButton Instance { get; private set; }

    public enum HintState
    {
        Tutorial,
        ExploreFirstArea,
        LampPuzzle,
        ObtainLiftKey,
        OpenLift,
        InLift,
        ExploreSecondArea,
        ElectricChair,
        GhostGirl,
        TeddyBear,
        Axe,
        Monster
    }

    public HintState hintState;
    public int hintNumber;
    public TextMeshProUGUI hintText;
    
    private void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        hintState = HintState.Tutorial;
        hintNumber = 1;
    }

    public void changeHintState(HintState newState) 
    {
        hintState = newState;
        hintNumber = 1;
    }

    public void pressHintButton()
    {
        switch (hintState)
        {
            case HintState.Tutorial:
            hintText.text = "I should complete the tutorial";
            break;

            case HintState.ExploreFirstArea:
            hintText.text = "I should explore some of the rooms in this place.";
            break;

            case HintState.LampPuzzle:
            if (hintNumber == 1) {
                hintText.text = "I wonder if I could turn that lamp on?";
                hintNumber++;
            }
            else if (hintNumber == 2) {
                hintText.text = "Is there a switch somewhere i could press?";
            }
            break;

            case HintState.ObtainLiftKey:
            if (hintNumber == 1)
            {
                hintText.text = "The lamp turned on! I'd better go check it out...";
                hintNumber++;
            }
            else if (hintNumber == 2)
            {
                hintText.text = "What could that code be used for?";
                hintNumber++;
            }
            else if (hintNumber == 3)
            {
                hintText.text = "I don't thing there's anything useful left in the office...";
            }
            break;

            case HintState.OpenLift:
            if (hintNumber == 1)
            {
                hintText.text = "Where could I use this card?";
                hintNumber++;
            }
            else if (hintNumber == 2)
            {
                hintText.text = "Lift key... have I seen a lift before?";
                hintNumber++;
            }
            else if (hintNumber == 3)
            {
                hintText.text = "Wasn't there a lift near the room I woke up in?";
            }
            break;

            case HintState.InLift:
            hintText.text = "Doesn't a lift usually have a button in it?";
            break;

            case HintState.ExploreSecondArea:
            hintText.text = "I should explore this area more.";
            break;

            case HintState.ElectricChair:
            hintText.text = "That date seems important, I should remember it.";
            break;

            case HintState.GhostGirl:
            if (hintNumber == 1)
            {
                hintText.text = "Poor girl... I should find what she lost.";
                hintNumber++;
            } 
            else if (hintNumber == 2) 
            {
                hintText.text = "There's a room with a padlock next door, I should look there.";
                hintNumber++;
            } 
            else if (hintNumber == 3)
            {
                hintText.text = "What was that date I saw earlier? Could that be the combination?";
            }
            break;

            case HintState.TeddyBear:
            hintText.text = "I should bring this back to that ghostly girl.";
            break;

            case HintState.Axe:
            hintText.text = "Time to go... I might be able to break something wooden with that axe.";
            break;

            case HintState.Monster:
            hintText.text = "I think that creature sees me when I move. I have to be careful.";
            break;
        }
    }



}
