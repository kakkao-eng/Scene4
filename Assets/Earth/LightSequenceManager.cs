using System.Collections.Generic;
using UHFPS.Runtime;
using UnityEngine;
using UnityEngine.UI; // หากใช้ Text ธรรมดา
// using TMPro; // หากใช้ TextMeshPro

public class LightSequenceManager : MonoBehaviour
{
    public SimpleLight[] Lights;
    private List<string> CorrectSequence = new List<string> { "R", "X", "P", "D" };
    private Dictionary<string, KeyCode> KeyBindings = new Dictionary<string, KeyCode>
    {
        { "R", KeyCode.Y },
        { "X", KeyCode.U },
        { "P", KeyCode.I },
        { "D", KeyCode.O }
    };

    public KeyCode resetKey = KeyCode.T;
    private int currentStep = 0;
    private bool firstPress = false;
    private float timeRemaining = 60f;
    private bool timerRunning = false;

    public Text timeText; // หากใช้ Text ธรรมดา
    // public TMP_Text timeText; // หากใช้ TextMeshPro

    public GameObject door; // ลาก GameObject ของประตูมาตรงนี้ใน Inspector

    public void ActivateLight(int lightIndex)
    {
        if (lightIndex < 0 || lightIndex >= Lights.Length)
        {
            Debug.Log("⚠️ หมายเลขไฟไม่ถูกต้อง!");
            return;
        }

        if (!firstPress)
        {
            firstPress = true;
            timerRunning = true;
        }

        if (lightIndex == currentStep)
        {
            Lights[lightIndex].SetLightState(true);
            Debug.Log($"✅ เปิดไฟ {CorrectSequence[lightIndex]} ที่ตำแหน่ง {currentStep}");
            currentStep++;

            if (currentStep >= CorrectSequence.Count)
            {
                Debug.Log("🎉 ลำดับไฟครบแล้ว! 🎉");
                timerRunning = false;
                door.SetActive(true); // เปิดประตู
            }
        }
        else
        {
            Debug.Log("❌ เปิดผิดลำดับ! รีเซ็ต...");
            ResetSequence();
        }
    }

    public void ResetGame()
    {
        Debug.Log("🔄 รีเซ็ตเกมใหม่!");
        ResetSequence();
    }

    private void ResetSequence()
    {
        foreach (var light in Lights)
        {
            light.SetLightState(false);
        }

        Debug.Log("🔄 รีเซ็ตลำดับไฟแล้ว!");
        currentStep = 0;
        firstPress = false;
        timeRemaining = 60f;
        timerRunning = false;

        if (door != null)
            door.SetActive(false); // ปิดประตูตอนเริ่มใหม่
    }

    void Update()
    {
        if (timerRunning)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0)
            {
                Debug.Log("⏳ หมดเวลา! รีเซ็ตลำดับ...");
                ResetSequence();
            }

            // แสดงเวลาใน UI
            if (timeText != null)
            {
                timeText.text = $"Time: {Mathf.Max(0f, timeRemaining):00.00}";
            }
        }

        if (Input.GetKeyDown(resetKey))
        {
            ResetGame();
        }

        foreach (var kvp in KeyBindings)
        {
            if (Input.GetKeyDown(kvp.Value))
            {
                int index = CorrectSequence.IndexOf(kvp.Key);
                if (index == -1) continue;

                if (index == currentStep)
                {
                    ActivateLight(index);
                }
                else
                {
                    Debug.Log($"❌ กด {kvp.Key} ผิดลำดับ! รีเซ็ต...");
                    ResetSequence();
                }
            }
        }
    }
}
