using UnityEngine;
using System.Collections;
using Entities;

public class SplashyBoi : MonoBehaviour {

    [SerializeField]
    protected float intensity;

    protected Vector2 speed;

    [SerializeField]
    protected Collider2D col;

    [SerializeField]
    protected bool inWater;

    public bool IsInWater {
        get {
            return inWater;
        }
    }

    public float Depth {
        get {
            return depth;
        }
    }

    [SerializeField]
    protected float minTimeBetweenSplash;

    protected float timeSinceSplash;

    protected Collider2D[] results;

    protected ContactFilter2D filter;

    protected float depth;

    // Use this for initialization
    void Start() {
        results = new Collider2D[1];

        filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("WaterCapture"));

        depth = 1000;
    }

    private void OnEnable() {
        speed = Vector2.down;
        timeSinceSplash = minTimeBetweenSplash;
        inWater = false;
    }

    protected void FixedUpdate() {
        timeSinceSplash += Time.fixedDeltaTime;
        var numCol = col.OverlapCollider(filter, results);

        if(numCol != 0) {
            for(int i = 0; i < numCol; i++) {
                var water = results[i].GetComponentInParent<WaterRenderer>();

                if(water != null) {
                    var pointDown = new Vector2(col.bounds.center.x, col.bounds.min.y);
                    var pointUp = new Vector2(col.bounds.center.x, col.bounds.max.y);
                    var height = water.GetHeightAt(pointDown);
                    depth = height - pointDown.y;
                    var colDown = height > pointDown.y;
                    var colUp = height > pointUp.y;

                    if(colDown) {
                        if(!inWater) {
                            inWater = true;
                            water.AddSplash(intensity * -speed.y, pointDown, speed);
                        }
                    } else if(inWater) {
                        inWater = false;
                        water.AddSplash(intensity * -speed.y, pointDown, speed);
                    }

                    if(timeSinceSplash > minTimeBetweenSplash) {
                        if(colDown && !colUp) {
                                timeSinceSplash = 0;
                                water.AddSplash(intensity * (Mathf.Abs(speed.x)) , pointDown, speed);
                        }
                    }
                }
            }
        } else {
            inWater = false;
        }
    }

    public void SetSpeed(Vector2 speed) {
        this.speed = speed;
    }

    public void SetCollider(Collider2D col) {
        this.col = col;
    }

}
