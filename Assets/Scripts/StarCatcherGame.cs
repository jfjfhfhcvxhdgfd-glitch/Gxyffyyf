using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Creates and runs the complete game so the scene needs no external assets.</summary>
public sealed class StarCatcherGame : MonoBehaviour
{
    private const float GameLength = 30f;
    private const float PlayWidth = 4.25f;
    private readonly List<FallingObject> fallingObjects = new List<FallingObject>();

    private Transform player;
    private Text scoreLabel;
    private Text timerLabel;
    private GameObject finishPanel;
    private Text finishLabel;
    private float timeLeft;
    private float spawnTimer;
    private int score;
    private bool isPlaying;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = 5f;
        CreateStars();
        CreatePlayer();
        CreateInterface();
        StartGame();
    }

    private void Update()
    {
        if (!isPlaying) return;

        MovePlayer();
        timeLeft -= Time.deltaTime;
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnObject();
            spawnTimer = Mathf.Lerp(0.72f, 0.28f, 1f - timeLeft / GameLength);
        }

        for (int i = fallingObjects.Count - 1; i >= 0; i--)
        {
            FallingObject item = fallingObjects[i];
            item.transform.position += Vector3.down * item.speed * Time.deltaTime;
            if (Vector2.Distance(item.transform.position, player.position) < 0.52f)
            {
                score = Mathf.Max(0, score + item.value);
                Destroy(item.gameObject);
                fallingObjects.RemoveAt(i);
            }
            else if (item.transform.position.y < -5.7f)
            {
                Destroy(item.gameObject);
                fallingObjects.RemoveAt(i);
            }
        }

        scoreLabel.text = "PUAN  " + score;
        timerLabel.text = Mathf.CeilToInt(Mathf.Max(0f, timeLeft)).ToString() + " SN";
        if (timeLeft <= 0f) EndGame();
    }

    private void MovePlayer()
    {
        float x = player.position.x;
        float keyboard = Input.GetAxisRaw("Horizontal");
        if (keyboard != 0f) x += keyboard * 8f * Time.deltaTime;
        else if (Input.GetMouseButton(0))
            x = Camera.main.ScreenToWorldPoint(Input.mousePosition).x;
        else if (Input.touchCount > 0)
            x = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position).x;
        player.position = new Vector3(Mathf.Clamp(x, -PlayWidth, PlayWidth), -4.15f, 0f);
    }

    private void SpawnObject()
    {
        bool meteor = Random.value < 0.28f;
        GameObject item = CreateShape(meteor ? "Meteor" : "Star", meteor ? new Color(1f, .25f, .3f) : new Color(1f, .84f, .18f), meteor ? .35f : .27f);
        item.transform.position = new Vector3(Random.Range(-PlayWidth, PlayWidth), 5.6f, 0f);
        item.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        fallingObjects.Add(item.AddComponent<FallingObject>().Initialize(meteor ? -3 : 1, meteor ? 3.8f : 3.1f));
    }

    private void CreatePlayer()
    {
        GameObject ship = CreateShape("Player", new Color(.18f, .75f, 1f), .42f);
        ship.transform.position = new Vector3(0f, -4.15f, 0f);
        ship.transform.localScale = new Vector3(1.25f, .75f, 1f);
        player = ship.transform;
    }

    private void CreateStars()
    {
        for (int i = 0; i < 36; i++)
        {
            GameObject dot = CreateShape("Background Star", new Color(.5f, .7f, 1f, .65f), Random.Range(.015f, .045f));
            dot.transform.position = new Vector3(Random.Range(-5.4f, 5.4f), Random.Range(-5f, 5f), 2f);
        }
    }

    private GameObject CreateShape(string objectName, Color color, float size)
    {
        GameObject go = new GameObject(objectName);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f));
        renderer.color = color;
        go.transform.localScale = Vector3.one * size;
        return go;
    }

    private void CreateInterface()
    {
        Canvas canvas = new GameObject("Interface", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule));
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(720f, 1280f);
        scoreLabel = CreateText(canvas.transform, "Score", new Vector2(30, -38), TextAnchor.UpperLeft, 36);
        timerLabel = CreateText(canvas.transform, "Timer", new Vector2(-30, -38), TextAnchor.UpperRight, 36);
        finishPanel = new GameObject("Finish Panel", typeof(Image));
        finishPanel.transform.SetParent(canvas.transform, false);
        RectTransform panel = finishPanel.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(.1f, .38f); panel.anchorMax = new Vector2(.9f, .62f); panel.offsetMin = panel.offsetMax = Vector2.zero;
        finishPanel.GetComponent<Image>().color = new Color(.04f, .08f, .18f, .96f);
        finishLabel = CreateText(finishPanel.transform, "Result", new Vector2(0, 45), TextAnchor.MiddleCenter, 42);
        Button button = new GameObject("Replay", typeof(Image), typeof(Button)).GetComponent<Button>();
        button.transform.SetParent(finishPanel.transform, false);
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(.2f, .08f); buttonRect.anchorMax = new Vector2(.8f, .42f); buttonRect.offsetMin = buttonRect.offsetMax = Vector2.zero;
        button.GetComponent<Image>().color = new Color(.15f, .68f, 1f, 1f);
        CreateText(button.transform, "Replay Label", Vector2.zero, TextAnchor.MiddleCenter, 30).text = "TEKRAR OYNA";
        button.onClick.AddListener(StartGame);
        finishPanel.SetActive(false);
    }

    private Text CreateText(Transform parent, string objectName, Vector2 position, TextAnchor alignment, int size)
    {
        Text text = new GameObject(objectName, typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(parent, false);
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = alignment == TextAnchor.UpperLeft ? new Vector2(0, 1) : alignment == TextAnchor.UpperRight ? new Vector2(1, 1) : new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = new Vector2(600, 110);
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = alignment; text.fontSize = size; text.color = Color.white; text.fontStyle = FontStyle.Bold;
        return text;
    }

    private void StartGame()
    {
        foreach (FallingObject item in fallingObjects) if (item != null) Destroy(item.gameObject);
        fallingObjects.Clear(); score = 0; timeLeft = GameLength; spawnTimer = .4f; isPlaying = true;
        if (finishPanel != null) finishPanel.SetActive(false);
    }

    private void EndGame()
    {
        isPlaying = false;
        finishLabel.text = "SÜRE BİTTİ!\nPUANIN: " + score;
        finishPanel.SetActive(true);
    }
}

public sealed class FallingObject : MonoBehaviour
{
    public int value { get; private set; }
    public float speed { get; private set; }
    public FallingObject Initialize(int points, float fallSpeed) { value = points; speed = fallSpeed; return this; }
}
