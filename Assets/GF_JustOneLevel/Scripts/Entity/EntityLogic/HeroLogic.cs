using System;
using System.Collections.Generic;
using System.Reflection;
using GameFramework;
using GameFramework.Event;
using GameFramework.Fsm;
using UnityEngine;

public class HeroLogic : TargetableObject {
    [SerializeField]
    private HeroData m_heroData = null;

    [SerializeField]
    private Rigidbody m_Rigidbody = null;

    /// <summary>
    /// 所有动画名称列表
    /// </summary>
    private List<string> m_AnimationNames = new List<string>();
    private GameFramework.Fsm.IFsm<HeroLogic> m_HeroFsm;

    protected override void OnInit (object userData) {
        base.OnInit (userData);

        m_Rigidbody = gameObject.GetComponent<Rigidbody>();

        /* 获取动画名称 */
        foreach (AnimationState state in gameObject.GetComponent<Animation> ()) {
            m_AnimationNames.Add (state.name);
        }

        /* 创建状态机 */
        List<FsmState<HeroLogic>> fsmStateList = new List<FsmState<HeroLogic>>();

        Type heroFSMStateType = typeof(FsmState<HeroLogic>);
        Assembly assembly = Assembly.GetExecutingAssembly();
        Type[] types = assembly.GetTypes();
        for (int i = 0; i < types.Length; i++)
        {
            if (!types[i].IsClass || types[i].IsAbstract)
            {
                continue;
            }

            if (types[i].IsSubclassOf(heroFSMStateType))
            {
                fsmStateList.Add((FsmState<HeroLogic>)Activator.CreateInstance(types[i]));
            }
        }

        m_HeroFsm = GameEntry.Fsm.CreateFsm<HeroLogic> (this, fsmStateList.ToArray());

        /* 启动站立状态 */
        m_HeroFsm.Start<HeroIdleState> ();

        /* 订阅事件 */
    }

    protected override void OnShow (object userData) {
        base.OnShow (userData);

        m_heroData = userData as HeroData;
        if (m_heroData == null) {
            Log.Error ("Hero data is invalid.");
            return;
        }
    }

    protected override void OnUpdate (float elapseSeconds, float realElapseSeconds) {
        base.OnUpdate (elapseSeconds, realElapseSeconds);

        /* 旋转镜头 */
        float inputHorizontal = Input.GetAxis ("Horizontal");
        if (inputHorizontal != 0) {
            CachedTransform.Rotate (new Vector3 (0, inputHorizontal * 0.8f * m_heroData.RotateSpeed, 0));
        }
    }

    protected override void OnHide (object userData) {
        base.OnHide (userData);

        GameEntry.Fsm.DestroyFsm<HeroLogic> ();
    }

    public override ImpactData GetImpactData () {
        return new ImpactData (m_heroData.Camp, m_heroData.HP, 0, m_heroData.Def);
    }

    /// <summary>
    /// 向前移动
    /// </summary>
    /// <param name="distance"></param>
    public void Forward (float distance) {
        // CachedTransform.position += CachedTransform.forward * distance * m_heroData.MoveSpeed;
        m_Rigidbody.MovePosition(CachedTransform.position + CachedTransform.forward * distance * m_heroData.MoveSpeed);
    }

    /// <summary>
    /// 是否在攻击范围内
    /// </summary>
    /// <param name="distance"></param>
    /// <returns></returns>
    public bool CheckInAtkRange(float distance) {
        return distance <= m_heroData.AtkRange;
    }

    /// <summary>
    /// 切换动画
    /// </summary>
    /// <param name="state"></param>
    public void ChangeAnimation (HeroAnimationState state) {
        Log.Info("ChangeAnimation");
        CachedAnimation.CrossFade (m_AnimationNames[(int) state], 0.01f);
    }

    /// <summary>
    /// 是否正在播放某个状态的动画
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    public bool IsPlayingAnimation(HeroAnimationState state) {
        return CachedAnimation.IsPlaying(m_AnimationNames[(int)state]);
    }

    /// <summary>
    /// 执行攻击
    /// </summary>
    /// <param name="aimEntity">攻击目标</param>
    public void PerformAttack(TargetableObject aimEntity) {
        aimEntity.ApplyDamage(this, m_heroData.Atk);
    }
}