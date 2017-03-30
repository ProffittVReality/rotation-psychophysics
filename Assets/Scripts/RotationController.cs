using System;
using UnityEngine;
using System.Collections.Generic;
using Valve.VR;

[RequireComponent(typeof(AudioSource))]
public class RotationController : MonoBehaviour
{
	private enum Option {
		RoomA = 0,
		RoomB = 1
	};

	public GameObject rotatingObject;
	public GameObject headset;
	public GUI_Handler gui;
	public float initialGain = 1f;
	public float maxTrialTime = 30;
	public KeyCode switchRoom = KeyCode.Z;
	public KeyCode selectA = KeyCode.X;
	public KeyCode selectB = KeyCode.C;
	public UnityEngine.UI.Text roomText;

	public float gainProportionMultiplier = 0.6f;
	public float gainDecrementMultiplier = 0.6f;
	public float gainDecrement = 0.05f;

	private float maxGainProportion = 1f;
	private float gainProportion = 1f;
	private int gainSign;

	private int failedBlocks = 0;
	private int maxFailedBlocks = 4;
	private bool started = false;

	private int trials = 0;
	private int correctTrials = 0;
	private int minTrialsToPass = 7;
	private int maxTrials = 10;

	private Option rotatingRoom;
	private Option selectedRoom;

	private AudioSource chimes;
	private bool chimesPlayed = false;
	private float timer = 0;

	public GameObject tracker;
	private Quaternion previousRotation;

	private System.Random random;

	void Start()
	{
		chimes = this.gameObject.GetComponent<AudioSource>();
		random = new System.Random();
		rotatingRoom = (Option)random.Next(2);
		selectedRoom = Option.RoomA;
		SetRoomText(selectedRoom);
		previousRotation = tracker.transform.rotation;
		gainSign = 2 * random.Next (2) - 1;
	}

	void Update()
	{
		if (!started)
			return;

		if (selectedRoom == rotatingRoom) {
			Vector3 deltaRotation = (tracker.transform.rotation * Quaternion.Inverse(previousRotation)).eulerAngles;

			if (deltaRotation.x > 180)
				deltaRotation.x = (deltaRotation.x - 360);
			if (deltaRotation.y > 180)
				deltaRotation.y = (deltaRotation.y - 360);
			if (deltaRotation.z > 180)
				deltaRotation.z = (deltaRotation.z - 360);

			rotatingObject.transform.Rotate (Vector3.down * deltaRotation.y * initialGain * gainProportion * gainSign, Space.World);
		}

		previousRotation = tracker.transform.rotation;

		timer += Time.deltaTime;
		if (timer > maxTrialTime && !chimesPlayed) {
			chimes.Play();
			chimesPlayed = true;
		}

		if (Input.GetKeyDown(switchRoom) && !gui.isVisible) //Change whether its rotating
		{
			selectedRoom = selectedRoom == Option.RoomA ? Option.RoomB : Option.RoomA;
			timer = 0;
			chimesPlayed = false;
			SetRoomText(selectedRoom);
			rotatingObject.transform.Rotate(Vector3.down * random.Next(360));
		}

		if (Input.GetKeyUp(selectA) || Input.GetKeyUp(selectB) && !gui.isVisible) //User answers
		{
			bool? correct = null;
			trials += 1;
			timer = 0;
			chimesPlayed = false;

			if (Input.GetKeyUp(selectA)) //A is rotating
			{
				correctTrials += rotatingRoom == Option.RoomA ? 1 : 0;
				correct = rotatingRoom == Option.RoomA;

				Debug.Log ("Selected A");
			}
			else if (Input.GetKeyUp(selectB)) //B is rotating
			{
				correctTrials += rotatingRoom == Option.RoomB ? 1 : 0;
				correct = rotatingRoom == Option.RoomB;

				Debug.Log ("Selected B");
			}

			Debug.Log(string.Format("Failed Trials: {0}, Number Answered: {1}, Number Right: {2}, Gain: {3}", failedBlocks, trials, correctTrials, initialGain * gainProportion * gainSign));
			gui.exportData(new List<string> {correct.ToString(), (initialGain * gainSign * gainProportion).ToString(), correctTrials.ToString(), gainDecrement.ToString()});

			gainProportion -= gainDecrement;

			if (trials >= maxTrials)
			{
				if (correctTrials >= minTrialsToPass)
				{
					maxGainProportion *= gainProportionMultiplier;
					gainDecrement *= gainDecrementMultiplier;
				}
				else
				{
					failedBlocks += 1;
				}

				gainProportion = maxGainProportion;
				trials = 0;
				correctTrials = 0;
			}

			rotatingRoom = (Option)random.Next(2);
			selectedRoom = Option.RoomA;
			SetRoomText(selectedRoom);
			gainSign *= -1;
			rotatingObject.transform.Rotate(Vector3.down * random.Next(360));

			if (failedBlocks >= maxFailedBlocks)
			{
				Application.Quit();
				UnityEditor.EditorApplication.isPlaying = false;
			}
		}
	}

	public void StartExperiment() {
		started = true;
		Debug.Log("Started");
		rotatingObject.transform.Rotate(Vector3.down * random.Next(360));
	}

	void SetRoomText(Option o) {
		if (o == Option.RoomA)
			roomText.text = "Room A";
		else if (o == Option.RoomB)
			roomText.text = "Room B";
	}
}
