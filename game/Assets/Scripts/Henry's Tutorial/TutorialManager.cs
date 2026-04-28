using System.Collections;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class TutorialManager : MonoBehaviour
{
    public bool tutorialCompleted = false;
    public bool tutorialStarted = false;

    public Animator leftHandAnimator;
    public Animator rightHandAnimator;
    
    UnityEngine.XR.InputDevice rightHand;
    UnityEngine.XR.InputDevice leftHand;
    public XRNode rightHandNode = XRNode.RightHand;
    public XRNode leftHandNode = XRNode.LeftHand;

    public TextMeshProUGUI tutorialText;
    public UnityEngine.UI.Button okButton;
    public GameObject okButtonObject;
    private bool okButtonPressed = false;

    public AudioSource successSound;

    public GameObject leftController;
    public GameObject rightController;
    public GameObject leftControllerGripArrow;
    public GameObject rightControllerGripArrow;
    public GameObject leftControllerTriggerArrow;
    public GameObject rightControllerTriggerArrow;
    public GameObject leftControllerMenuArrow;
    public GameObject rightControllerAButtonArrow;


    public static TutorialManager Instance { get; private set; }

    //tutorial stages:
    // 1: grip
    // 2: push
    // 3: turn left
    // 4: turn right
    // 5: braking, trigger to move on
    // 6: menu
    // 7: breathing rate information, trigger to move on
    // 8: item stuff

    private void Awake()
    {
        rightHand = InputDevices.GetDeviceAtXRNode(rightHandNode);
        leftHand = InputDevices.GetDeviceAtXRNode(leftHandNode);
        Instance = this;
    }

    void Start()
    {
        okButtonObject.SetActive(false);
        okButton.onClick.AddListener(() => { okButtonPressed = true; });
        leftControllerGripArrow.SetActive(false);
        rightControllerGripArrow.SetActive(false);
        leftControllerTriggerArrow.SetActive(false);
        rightControllerTriggerArrow.SetActive(false);
        leftControllerMenuArrow.SetActive(false);
        rightControllerAButtonArrow.SetActive(false);
        StartCoroutine(Tutorial());
    }

    public IEnumerator Tutorial()
    {
        tutorialStarted = true;
        // Tutorial Step 1: Grip the controllers
        yield return StartCoroutine(GripTutorial());

        // Tutorial Step 2: Move the chair
        yield return StartCoroutine(MoveTutorial());

        // Tutorial Step 3: Turn the chair
        yield return StartCoroutine(TurnTutorial());

        // Tutorial Step 4: Open the menu
        yield return StartCoroutine(MenuTutorial());

        // Tutorial Step 5: Breathing information
        yield return StartCoroutine(BreathingInformation());

        // Tutorial Step 6: Item interaction
        yield return StartCoroutine(ItemInteraction());

        tutorialCompleted = true;
        HintButton.Instance.changeHintState(HintButton.HintState.ExploreFirstArea);
    }

    public IEnumerator GripTutorial()
    {
        //enable step 1 on the tutorial canvas
        tutorialText.text = "Press down both grip buttons to grab your wheels.";

        leftControllerGripArrow.SetActive(true);
        rightControllerGripArrow.SetActive(true);

        //begin the animation for the controllers
        leftHandAnimator.SetInteger("LeftTutorialStage", 1);
        rightHandAnimator.SetInteger("RightTutorialStage", 1);

        //wait for the player to grip the controllers
        rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool rightGripPressed);
        leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool leftGripPressed);

        while (!rightGripPressed || !leftGripPressed)
        {
            rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out rightGripPressed);
            leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out leftGripPressed);
            yield return null;
        }

        successSound.Play();
        yield return new WaitForSeconds(2f);
    }

    public IEnumerator MoveTutorial()
    {
        
        tutorialText.text = "While gripping, push both controllers forward/backwards and let go to move in that direction.";

        while (MovementController.Instance.currentSpeed == 0) yield return null;

        successSound.Play();
        yield return new WaitForSeconds(2f);
        
        tutorialText.text = "Gripping and not moving your hands will stop the wheelchair.";

        okButtonObject.SetActive(true);

        yield return new WaitUntil(() => okButtonPressed);

        okButtonPressed = false;
        okButtonObject.SetActive(false);

    }

    public IEnumerator TurnTutorial()
    {
        
        tutorialText.text = "To turn left, push the right controller forward and pull the left controller backwards while gripping. To turn right, do the reverse.";

        MovementController.Instance.ListenForMovement();
        while (MovementController.Instance.rotated == false) yield return null;

        successSound.Play();
        yield return new WaitForSeconds(2f);
    }

    public IEnumerator MenuTutorial()
    {
        leftHandAnimator.SetInteger("LeftTutorialStage", 2);
        rightHandAnimator.SetInteger("RightTutorialStage", 2);

        leftControllerGripArrow.SetActive(false);
        rightControllerGripArrow.SetActive(false);
        leftControllerMenuArrow.SetActive(true);
        leftController.transform.eulerAngles = new Vector3(-90, 0, 0);

        tutorialText.text = "Press the menu button on your left controller to open the pause menu.";
        
        leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton, out bool menuButtonPressed);
        while (!menuButtonPressed)
        {
            leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton, out menuButtonPressed);
            yield return null;
        }

        successSound.Play();

    }

    public IEnumerator BreathingInformation()
    {
        okButtonObject.SetActive(true);

        tutorialText.text = "Due to the toxicity of the air, you must pick up the oxygen tank and blood toxicity meter in the room.";

        yield return new WaitUntil(() => okButtonPressed);
        okButtonPressed = false;

        tutorialText.text = "Breathing consumes oxygen. You can refill your tank in safe zones.";

        yield return new WaitUntil(() => okButtonPressed);
        okButtonPressed = false;

        tutorialText.text = "You must breathe to decrease your blood toxicity. If your blood toxicity gets too high, you will begin to hallucinate.";

        yield return new WaitUntil(() => okButtonPressed);
        okButtonPressed = false;

        tutorialText.text = "If you run out of oxygen, or if your blood toxicity gets too high, you will pass out.";

        yield return new WaitUntil(() => okButtonPressed);
        okButtonPressed = false;
    }

    public IEnumerator ItemInteraction()
    {
        tutorialText.text = "Objects with particles can be interacted with. Some items can be picked up.";

        yield return new WaitUntil(() => okButtonPressed);
        okButtonPressed = false;

        okButtonObject.SetActive(false);

        rightHandAnimator.SetInteger("RightTutorialStage", 3);
        leftHandAnimator.SetInteger("LeftTutorialStage", 3);

        leftControllerMenuArrow.SetActive(false);
        rightControllerTriggerArrow.SetActive(true);
        leftControllerTriggerArrow.SetActive(true);
        leftController.transform.eulerAngles = new Vector3(0, 180, 0);
        rightController.transform.eulerAngles = new Vector3(0, 180, 0);

        tutorialText.text = "Point your controller at the key, oxygen tank and blood toxicity meter and press the trigger to pick them up.";
        while (!InventoryManager.Instance.itemInInventory()) yield return null;

        successSound.Play();
        yield return new WaitForSeconds(2f);

        rightHandAnimator.SetInteger("RightTutorialStage", 4);
        leftHandAnimator.SetInteger("LeftTutorialStage", 4);

        rightControllerTriggerArrow.SetActive(false);
        leftControllerTriggerArrow.SetActive(false);
        rightControllerAButtonArrow.SetActive(true);
        leftController.transform.eulerAngles = new Vector3(0, 0, 0);
        rightController.transform.eulerAngles = new Vector3(-90, 0, 0);

        tutorialText.text = "Press the A button on the right controller to cycle through your inventory items";

        while (InventoryManager.Instance.getActiveItemID() != "doorkey") yield return null;

        successSound.Play();
        yield return new WaitForSeconds(2f);

        rightControllerAButtonArrow.SetActive(false);

        tutorialText.text = "You have completed the tutorial. Hold the key to the lock to unlock the door, and continue to escape the hospital!";

        rightController.SetActive(false);
        leftController.SetActive(false);
    }

}
