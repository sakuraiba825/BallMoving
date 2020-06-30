using System;
using xna = Microsoft.Xna.Framework;
using URWPGSim2D.Common;
using URWPGSim2D.StrategyLoader;
using method = URWPGSim2D.Strategy.method;

namespace URWPGSim2D.Strategy
{
    public class Strategy : MarshalByRefObject, IStrategy
    {
        #region reserved code never be changed or removed
        /// <summary>
        /// override the InitializeLifetimeService to return null instead of a valid ILease implementation
        /// to ensure this type of remote object never dies
        /// </summary>
        /// <returns>null</returns>
        public override object InitializeLifetimeService()
        {
            //return base.InitializeLifetimeService();
            return null; // makes the object live indefinitely
        }
        #endregion

        /// <summary>
        /// 决策类当前对象对应的仿真使命参与队伍的决策数组引用 第一次调用GetDecision时分配空间
        /// </summary>
        private Decision[] decisions = null;

        /// <summary>
        /// 获取队伍名称 在此处设置参赛队伍的名称
        /// </summary>
        /// <returns>队伍名称字符串</returns>
        public string GetTeamName()
        {
            CalHol();
            return "搬运工";
        }

        #region 获得球洞的位置坐标
        private xna.Vector3[] hole = new xna.Vector3[6];
        private void CalHol()
        {
            for (int i = 1; i < 4; i++)
                for (int j = 1; j <= i; j++)
                {
                    int l = i * (i - 1) / 2 + j - 1;
                    hole[l].Y = 0;
                    hole[l].X = -(i * 3 - 3) * 80;
                    hole[l].Z = -(i - 1) * 2 * 80 + 4 * (j - 1) * 80 - 10;
                }
        }
        #endregion

        /// <summary>
        /// 获取当前仿真使命（比赛项目）当前队伍所有仿真机器鱼的决策数据构成的数组
        /// </summary>
        /// <param name="mission">服务端当前运行着的仿真使命Mission对象</param>
        /// <param name="teamId">当前队伍在服务端运行着的仿真使命中所处的编号 
        /// 用于作为索引访问Mission对象的TeamsRef队伍列表中代表当前队伍的元素</param>
        /// <returns>当前队伍所有仿真机器鱼的决策数据构成的Decision数组对象</returns>

        #region 每个球的阶段性标志
        bool t51 = false;
        bool t52 = false;
        bool t41 = false;
        bool t42 = false;
        bool t31 = false;
        bool t32 = false;
        bool t11 = false;
        bool t12 = false;
        bool t01 = false;
        bool t02 = false;
        bool t21 = false;
        bool t22 = false;
        int isMovingFlag0 = 0;//0号球正在被那条鱼搬运，1或者2
        int isMovingFlag4 = 0;//4号球正在被那条鱼搬运，1或者2
        int isMovingFlag1 = 0;
        int isMovingFlag2 = 0;
        int isMovingFlag5 = 0;
        int isMovingFlag3 = 0;
        bool fast5 = false;
        bool fast3 = false;

        #endregion

        public Decision[] GetDecision(Mission mission, int teamId)
        {
            // 决策类当前对象第一次调用GetDecision时Decision数组引用为null
            if (decisions == null)
            {// 根据决策类当前对象对应的仿真使命参与队伍仿真机器鱼的数量分配决策数组空间
                decisions = new Decision[mission.CommonPara.FishCntPerTeam];
            }

            #region 决策计算过程 需要各参赛队伍实现的部分
            #region 策略编写帮助信息
            //====================我是华丽的分割线====================//
            //======================策略编写指南======================//
            //1.策略编写工作直接目标是给当前队伍决策数组decisions各元素填充决策值
            //2.决策数据类型包括两个int成员，VCode为速度档位值，TCode为转弯档位值
            //3.VCode取值范围0-14共15个整数值，每个整数对应一个速度值，速度值整体但非严格递增
            //有个别档位值对应的速度值低于比它小的档位值对应的速度值，速度值数据来源于实验
            //4.TCode取值范围0-14共15个整数值，每个整数对应一个角速度值
            //整数7对应直游，角速度值为0，整数6-0，8-14分别对应左转和右转，偏离7越远，角度速度值越大
            //5.任意两个速度/转弯档位之间切换，都需要若干个仿真周期，才能达到稳态速度/角速度值
            //目前运动学计算过程决定稳态速度/角速度值接近但小于目标档位对应的速度/角速度值
            //6.决策类Strategy的实例在加载完毕后一直存在于内存中，可以自定义私有成员变量保存必要信息
            //但需要注意的是，保存的信息在中途更换策略时将会丢失
            //====================我是华丽的分割线====================//
            //=======策略中可以使用的比赛环境信息和过程信息说明=======//
            //场地坐标系: 以毫米为单位，矩形场地中心为原点，向右为正X，向下为正Z
            //            负X轴顺时针转回负X轴角度范围为(-PI,PI)的坐标系，也称为世界坐标系
            //mission.CommonPara: 当前仿真使命公共参数
            //mission.CommonPara.FishCntPerTeam: 每支队伍仿真机器鱼数量
            //mission.CommonPara.MsPerCycle: 仿真周期毫秒数
            //mission.CommonPara.RemainingCycles: 当前剩余仿真周期数
            //mission.CommonPara.TeamCount: 当前仿真使命参与队伍数量
            //mission.CommonPara.TotalSeconds: 当前仿真使命运行时间秒数
            //mission.EnvRef.Balls: 
            //当前仿真使命涉及到的仿真水球列表，列表元素的成员意义参见URWPGSim2D.Common.Ball类定义中的注释
            //mission.EnvRef.FieldInfo: 
            //当前仿真使命涉及到的仿真场地，各成员意义参见URWPGSim2D.Common.Field类定义中的注释
            //mission.EnvRef.ObstaclesRect: 
            //当前仿真使命涉及到的方形障碍物列表，列表元素的成员意义参见URWPGSim2D.Common.RectangularObstacle类定义中的注释
            //mission.EnvRef.ObstaclesRound:
            //当前仿真使命涉及到的圆形障碍物列表，列表元素的成员意义参见URWPGSim2D.Common.RoundedObstacle类定义中的注释
            //mission.TeamsRef[teamId]:
            //决策类当前对象对应的仿真使命参与队伍（当前队伍）
            //mission.TeamsRef[teamId].Para:
            //当前队伍公共参数，各成员意义参见URWPGSim2D.Common.TeamCommonPara类定义中的注释
            //mission.TeamsRef[teamId].Fishes:
            //当前队伍仿真机器鱼列表，列表元素的成员意义参见URWPGSim2D.Common.RoboFish类定义中的注释
            //mission.TeamsRef[teamId].Fishes[i].PositionMm和PolygonVertices[0],BodyDirectionRad,VelocityMmPs,
            //                                   AngularVelocityRadPs,Tactic:
            //当前队伍第i条仿真机器鱼鱼体矩形中心和鱼头顶点在场地坐标系中的位置（用到X坐标和Z坐标），鱼体方向，速度值，
            //                                   角速度值，决策值
            //====================我是华丽的分割线====================//
            //========================典型循环========================//
            //for (int i = 0; i < mission.CommonPara.FishCntPerTeam; i++)
            //{
            //  decisions[i].VCode = 0; // 静止
            //  decisions[i].TCode = 7; // 直游
            //}
            //====================我是华丽的分割线====================//
            #endregion
            //请从这里开始编写代码

            //获取鱼的对象
            RoboFish fish1 = mission.TeamsRef[teamId].Fishes[0];
            RoboFish fish2 = mission.TeamsRef[teamId].Fishes[1];

            #region 球是否进入洞的标志，1表示进入，0表示未进入
            int b0 = Convert.ToInt32(mission.HtMissionVariables["Ball0InHole"]);
            int b1 = Convert.ToInt32(mission.HtMissionVariables["Ball1InHole"]);
            int b2 = Convert.ToInt32(mission.HtMissionVariables["Ball2InHole"]);
            int b3 = Convert.ToInt32(mission.HtMissionVariables["Ball3InHole"]);
            int b4 = Convert.ToInt32(mission.HtMissionVariables["Ball4InHole"]);
            int b5 = Convert.ToInt32(mission.HtMissionVariables["Ball5InHole"]);
            #endregion

            #region 获取鱼的头部和身体中心点
            xna.Vector3 body1 = fish1.PositionMm; // 1号鱼的身体
            xna.Vector3 body2 = fish2.PositionMm; // 2号鱼的身体
            xna.Vector3 head1 = fish1.PolygonVertices[0];  // 1号鱼头部
            xna.Vector3 head2 = fish2.PolygonVertices[0];  // 2号鱼头部
            #endregion

            #region 获取球的位置点
            xna.Vector3 ball5 = mission.EnvRef.Balls[5].PositionMm;
            xna.Vector3 ball3 = mission.EnvRef.Balls[3].PositionMm;
            xna.Vector3 ball2 = mission.EnvRef.Balls[2].PositionMm;
            xna.Vector3 ball0 = mission.EnvRef.Balls[0].PositionMm;
            xna.Vector3 ball1 = mission.EnvRef.Balls[1].PositionMm;
            xna.Vector3 ball4 = mission.EnvRef.Balls[4].PositionMm;
            #endregion

            #region 运每个球的目标方向
            float desRad5 = StrategyHelper.Helpers.GetAngleDegree(hole[5] - ball5);//角度值              
            float desRad3 = StrategyHelper.Helpers.GetAngleDegree(hole[3] - ball3);
            float desRad0 = StrategyHelper.Helpers.GetAngleDegree(hole[0] - ball0);
            float desRad4 = StrategyHelper.Helpers.GetAngleDegree(hole[4] - ball4);
            float desRad1 = StrategyHelper.Helpers.GetAngleDegree(hole[1] - ball1);
            float desRad2 = StrategyHelper.Helpers.GetAngleDegree(hole[2] - ball2);
            #endregion


            #region 4号球 1号鱼
            if (b4 == 0)
            {
                //游动到球右下方定点（+100,0,+100）
                if (t41 == false)
                {
                    int times = 0;
                    xna.Vector3 desPoint = new xna.Vector3(ball5.X + 450, 0, ball5.Z + 210);
                    StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, desPoint, -desRad4, 10f, 10f,
                        mission.CommonPara.MsPerCycle, ref times);
                    if (method.GetDistance(head1,desPoint) > 200)
                    {
                        decisions[1].VCode += 1;
                    }                  
                }
                if (method.GetDistance(head1, hole[4]) - 80 > method.GetDistance(ball4, hole[4]) /*&& method.GetDistance(head1, ball5) < 120*/)
                {
                    t41 = true;
                }
                //调整至顶球点
                if (t41 == true)
                {
                    isMovingFlag4 = 1;
                    int times = 0;
                    xna.Vector3 desPoint = method.GetPointOfStart(ball4, hole[4]);
                    StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, desPoint, desRad4, 2f, 4f,
                        mission.CommonPara.MsPerCycle, ref times);//阈值 改小一点
                    t42 = true;
                }
                //开始顶球
                if (t41 == true && t42 == true)
                {
                    if (method.GetDistance(head1, method.GetPointOfStart(ball4, hole[4])) < 20 && Math.Abs(xna.MathHelper.ToDegrees(fish1.BodyDirectionRad) - desRad4) < 25)
                    {
                        if (method.GetDistance(ball4, hole[4]) > 200)
                        {
                            StrategyHelper.Helpers.Dribble(ref decisions[0], fish1, hole[4], desRad4,
                                8f, 3f, 200f, 12, 4, 10, 100, false);
                        }
                        else
                        {
                            StrategyHelper.Helpers.Dribble(ref decisions[0], fish1, hole[4], desRad4,
                                4f, 3f, 20f, 8, 6, 7, 100, false);
                        }

                    }
                    else
                    {

                        //顶球过程中微调
                        if (method.GetDistance(head1, hole[4]) + 10 < method.GetDistance(hole[4], ball4) && method.GetDistance(head1, ball4) < 120)
                        {
                            decisions[0].TCode = 7;
                            decisions[0].VCode = 13;
                        }
                        else
                        {
                            int times = 0;
                            xna.Vector3 desPoint = method.GetPointOfStart(ball4, hole[4]);
                            StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, desPoint, desRad4, 2f, 5f,
                                mission.CommonPara.MsPerCycle, ref times);
                        }
                    }
                }
            }
            #endregion

            #region 3号球 2号鱼
            if (b3 == 0)
            {
                //游动到球右上方定点（+150,0,-100）
                if (t31 == false)
                {
                    int times = 0;
                    xna.Vector3 desPoint = new xna.Vector3(ball3.X +300, 0, ball3.Z - 150);
                    StrategyHelper.Helpers.PoseToPose(ref decisions[1], fish2, desPoint, -desRad3, 10f, 10f,
                        mission.CommonPara.MsPerCycle, ref times);
                    if (method.GetDistance(head2, ball3) > 200)
                    {
                        decisions[1].VCode += 1;
                    }
                }
                if (method.GetDistance(head2, hole[3]) - 70 > method.GetDistance(ball3, hole[3]) /*&& method.GetDistance(head2, ball3) < 120*/)
                {
                    t31 = true;
                }
                //调整至顶球点
                if (t31 == true)
                {
                    isMovingFlag3 = 2;
                    int times = 0;
                    xna.Vector3 desPoint = method.GetPointOfStart(ball3, hole[3]);
                    StrategyHelper.Helpers.PoseToPose(ref decisions[1], fish2, desPoint, desRad3, 2f, 4f,
                        mission.CommonPara.MsPerCycle, ref times);//阈值 改小一点
                    t32 = true;
                }
                //开始顶球
                if (t31 == true && t32 == true)
                {
                    if (method.GetDistance(head2, method.GetPointOfStart(ball3, hole[3])) < 20 && Math.Abs(xna.MathHelper.ToDegrees(fish2.BodyDirectionRad) - desRad3) < 25)
                    {
                        if (method.GetDistance(ball3, hole[3]) > 200)
                        {
                            StrategyHelper.Helpers.Dribble(ref decisions[1], fish2, hole[3], desRad3,
                                8f, 3f, 200f, 12, 4, 10, 100, false);
                        }
                        else
                        {
                            StrategyHelper.Helpers.Dribble(ref decisions[1], fish2, hole[3], desRad3,
                                4f, 3f, 20f, 8, 4, 8, 100, false);
                        }

                    }
                    else
                    {

                        //顶球过程中微调   
                        if (method.GetDistance(head2, hole[3]) + 10 < method.GetDistance(hole[3], ball3) && method.GetDistance(head2, ball3) < 120)
                        {
                            decisions[1].TCode = 8;
                            decisions[1].VCode = 14;
                        }
                        else
                        {
                            int times = 0;
                            xna.Vector3 desPoint = method.GetPointOfStart(ball3, hole[3]);
                            StrategyHelper.Helpers.PoseToPose(ref decisions[1], fish2, desPoint, desRad3, 2f, 5f,
                                mission.CommonPara.MsPerCycle, ref times);
                        }
                    }
                }
            }
            #endregion

            #region 5号球 1号鱼
            if (b4 == 1 && b5 == 0)
            {
 /*               int times = 0;
                xna.Vector3 temppoint = new xna.Vector3(-100, 0, 110);
                StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, temppoint, desRad5, 2f, 5f,
                        mission.CommonPara.MsPerCycle, ref times);
                //method.approachToPoint(ref decisions[0], fish1, temppoint, 12, 8, 6,5);
               if (head1.X > -80)
                {
                    fast5 = true;
                }
*/
                //游动到球右下方定点（+85,0,+85）
                if (t51 == false /*&& fast5 == true*/)
                {
                    int times = 0;
                    xna.Vector3 desPoint = new xna.Vector3(ball5.X + 85, 0, ball5.Z + 90);
                    StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, desPoint, -desRad5, 10f, 10f,
                        mission.CommonPara.MsPerCycle, ref times);
                    if (method.GetDistance(head1, ball5) > 200)
                    {
                        decisions[0].VCode += 1;
                    }
                }
                if (method.GetDistance(head1, hole[5]) - 70 > method.GetDistance(ball5, hole[5]) /*&& method.GetDistance(head1, ball2) < 120*/)
                {
                    t51 = true;
                }
                //调整至顶球点
                if (t51 == true)
                {
                    isMovingFlag2 = 1;
                    int times = 0;
                    xna.Vector3 desPoint = method.GetPointOfStart(ball5, hole[5]);
                    StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, desPoint, desRad5, 2f, 5f,
                        mission.CommonPara.MsPerCycle, ref times);//阈值 改小一点
                    t22 = true;
                }
                //开始顶球
                if (t51 == true && t52 == true)
                {
                    if (method.GetDistance(head1, method.GetPointOfStart(ball5, hole[5])) < 20 && Math.Abs(xna.MathHelper.ToDegrees(fish1.BodyDirectionRad) - desRad5) < 25)
                    {
                        if (method.GetDistance(ball5, hole[5]) > 200)
                        {
                            StrategyHelper.Helpers.Dribble(ref decisions[0], fish1, hole[5], desRad5,
                                8f, 3f, 200f, 12, 4, 10, 100, false);
                        }
                        else
                        {
                            StrategyHelper.Helpers.Dribble(ref decisions[0], fish1, hole[5], desRad5,
                                4f, 3f, 20f, 8, 4, 8, 100, false);
                        }

                    }
                    else
                    {

                        //顶球过程中微调   
                        if (method.GetDistance(head1, hole[5]) + 10 < method.GetDistance(hole[5], ball5) && method.GetDistance(head1, ball5) < 120)
                        {
                            decisions[0].TCode = 7;
                            decisions[0].VCode = 13;
                        }
                        else
                        {
                            int times = 0;
                            xna.Vector3 desPoint = method.GetPointOfStart(ball5, hole[5]);
                            StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, desPoint, desRad5, 2f, 5f,
                                mission.CommonPara.MsPerCycle, ref times);
                        }
                    }
                }
            }
            #endregion

            #region 1号球 2号鱼
            if (b3 == 1 && b1 == 0)
            {
                //游动到球右上方定点（+85,0,-85）
                if (t11 == false)
                {
                    int times = 0;
                    xna.Vector3 desPoint = new xna.Vector3(ball1.X + 85, 0, ball1.Z - 90);
                    StrategyHelper.Helpers.PoseToPose(ref decisions[1], fish2, desPoint, -desRad1, 10f, 10f,
                        mission.CommonPara.MsPerCycle, ref times);
                    if (method.GetDistance(head2, ball1) > 200)
                    {
                        decisions[1].VCode += 1;
                    }
                }
                if (method.GetDistance(head2, hole[1]) - 70 > method.GetDistance(ball1, hole[1]) /*&& method.GetDistance(head2, ball1) < 120*/)
                {
                    t11 = true;
                }
                //调整至顶球点
                if (t11 == true)
                {
                    isMovingFlag1 = 2;
                    int times = 0;
                    xna.Vector3 desPoint = method.GetPointOfStart(ball1, hole[1]);
                    StrategyHelper.Helpers.PoseToPose(ref decisions[1], fish2, desPoint, desRad1, 2f, 5f,
                        mission.CommonPara.MsPerCycle, ref times);//阈值 改小一点
                    t12 = true;
                }
                //开始顶球
                if (t11 == true && t12 == true)
                {
                    if (method.GetDistance(head2, method.GetPointOfStart(ball1, hole[1])) < 20 && Math.Abs(xna.MathHelper.ToDegrees(fish2.BodyDirectionRad) - desRad1) < 25)
                    {
                        if (method.GetDistance(ball1, hole[1]) > 200)
                        {
                            StrategyHelper.Helpers.Dribble(ref decisions[1], fish2, hole[1], desRad1,
                                8f, 3f, 200f, 12, 4, 10, 100, false);
                        }
                        else
                        {
                            StrategyHelper.Helpers.Dribble(ref decisions[1], fish2, hole[1], desRad1,
                                4f, 3f, 20f, 8, 6, 8, 100, false);
                        }

                    }
                    else
                    {

                        //顶球过程中微调   
                        if (method.GetDistance(head2, hole[1]) + 10 < method.GetDistance(hole[1], ball1) && method.GetDistance(head2, ball1) < 120)
                        {
                            decisions[1].TCode = 6;
                            decisions[1].VCode = 14;
                        }
                        else
                        {
                            int times = 0;
                            xna.Vector3 desPoint = method.GetPointOfStart(ball1, hole[1]);
                            StrategyHelper.Helpers.PoseToPose(ref decisions[1], fish2, desPoint, desRad1, 2f, 5f,
                                mission.CommonPara.MsPerCycle, ref times);
                        }
                    }
                }
            }
            #endregion

            #region 2号球 1号鱼
            if (b5 == 1 && b2 == 0)
            {
                int times = 0;
                xna.Vector3 temppoint = new xna.Vector3(-260, 0, 210);
                StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, temppoint, desRad2, 2f, 5f,
                        mission.CommonPara.MsPerCycle, ref times);
                //method.approachToPoint(ref decisions[0], fish1, temppoint, 12, 8, 6,5);
                if (head1.X > -240)
                {
                    fast5 = true;
                }
                //游动到球右下方定点（+85,0,+85）
                if (t21 == false)
                {
                    // int times = 0;
                    xna.Vector3 desPoint = new xna.Vector3(ball2.X + 85, 0, ball2.Z + 90);
                    StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, desPoint, -desRad2, 10f, 10f,
                        mission.CommonPara.MsPerCycle, ref times);
                    if (method.GetDistance(head1, ball2) > 200)
                    {
                        decisions[0].VCode += 3;
                    }
                }
                if (method.GetDistance(head1, hole[2]) - 70 > method.GetDistance(ball2, hole[2])/*&& method.GetDistance(head1, ball2) < 120*/)
                {
                    t21 = true;
                }
                //调整至顶球点
                if (t21 == true)
                {
                    isMovingFlag2 = 1;
                    //int times = 0;
                    xna.Vector3 desPoint = method.GetPointOfStart(ball2, hole[2]);
                    StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, desPoint, desRad2, 2f, 5f,
                        mission.CommonPara.MsPerCycle, ref times);//阈值 改小一点
                    t22 = true;
                }
                //开始顶球
                if (t21 == true && t22 == true)
                {
                    if (method.GetDistance(head1, method.GetPointOfStart(ball2, hole[2])) < 20 && Math.Abs(xna.MathHelper.ToDegrees(fish1.BodyDirectionRad) - desRad2) < 25)
                    {
                        if (method.GetDistance(ball2, hole[2]) > 200)
                        {
                            StrategyHelper.Helpers.Dribble(ref decisions[0], fish1, hole[2], desRad2,
                                8f, 3f, 200f, 12, 4, 10, 100, false);
                        }
                        else
                        {
                            StrategyHelper.Helpers.Dribble(ref decisions[0], fish1, hole[2], desRad2,
                                4f, 3f, 20f, 8, 6, 8, 100, false);
                        }

                    }
                    else
                    {

                        //顶球过程中微调   
                        if (method.GetDistance(head1, hole[2]) + 10 < method.GetDistance(hole[2], ball2) && method.GetDistance(head1, ball2) < 120)
                        {
                            decisions[0].TCode = 6;
                            decisions[0].VCode = 14;
                        }
                        else
                        {
                            //int times = 0;
                            xna.Vector3 desPoint = method.GetPointOfStart(ball2, hole[2]);
                            StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, desPoint, desRad2, 2f, 5f,
                                mission.CommonPara.MsPerCycle, ref times);
                        }
                    }
                }
            }
            #endregion

            #region 0号球 2号鱼
            if (b3 == 1 && b1 == 1 && b0 == 0 && (isMovingFlag0 == 0 || isMovingFlag0 == 2))
            {
                //游动到球右上方定点（+85,0,-85）
                if (t01 == false)
                {
                    int times = 0;
                    xna.Vector3 desPoint = new xna.Vector3(ball0.X + 85, 0, ball0.Z - 90);
                    StrategyHelper.Helpers.PoseToPose(ref decisions[1], fish2, desPoint, -desRad0, 10f, 10f,
                        mission.CommonPara.MsPerCycle, ref times);
                    if (method.GetDistance(head2, ball0) > 200)
                    {
                        decisions[1].VCode += 1;
                    }
                }
                if (method.GetDistance(head2, hole[0]) - 70 > method.GetDistance(ball0, hole[0]) /*&& method.GetDistance(head2, ball0) < 120*/)
                {
                    t01 = true;
                }
                //调整至顶球点
                if (t01 == true)
                {   //开始调整至顶球点的时候，将0号球正在搬运的标志设为2
                    isMovingFlag0 = 2;
                    int times = 0;
                    xna.Vector3 desPoint = method.GetPointOfStart(ball0, hole[0]);
                    StrategyHelper.Helpers.PoseToPose(ref decisions[1], fish2, desPoint, desRad0, 2f, 5f,
                        mission.CommonPara.MsPerCycle, ref times);//阈值 改小一点
                    t02 = true;
                }
                //开始顶球
                if (t01 == true && t02 == true)
                {
                    if (method.GetDistance(head2, method.GetPointOfStart(ball0, hole[0])) < 20 && Math.Abs(xna.MathHelper.ToDegrees(fish2.BodyDirectionRad) - desRad0) < 25)
                    {
                        if (method.GetDistance(ball0, hole[0]) > 200)
                        {
                            StrategyHelper.Helpers.Dribble(ref decisions[1], fish2, hole[0], desRad0,
                                8f, 3f, 200f, 12, 4, 10, 100, false);
                        }
                        else
                        {
                            StrategyHelper.Helpers.Dribble(ref decisions[1], fish2, hole[0], desRad0,
                                4f, 3f, 20f, 8, 4, 8, 100, false);
                        }

                    }
                    else
                    {
                        //超过洞，快速调整
                        //if (((head2.X > hole[0].X && ball0.X < hole[0].X + 0)/* || (head2.X < hole[0].X && ball0.X > hole[0].X - 0)*/) && method.GetDistance(hole[0], head2) < 130)
                        //{
                        //    if (ball0.Z < hole[0].Z)
                        //    {
                        //        decisions[1].TCode = 0;
                        //        decisions[1].VCode += 1;
                        //    }
                        //    else
                        //    {
                        //        decisions[1].TCode = 14;
                        //       decisions[1].VCode += 1;
                        //    }
                        //}
                        //顶球过程中微调
                        if (method.GetDistance(head2, hole[0]) + 10 < method.GetDistance(hole[0], ball0) && method.GetDistance(head2, ball0) < 120)
                        {
                            decisions[1].TCode = 7;
                            decisions[1].VCode = 13;
                        }
                        else
                        {
                            int times = 0;
                            xna.Vector3 desPoint = method.GetPointOfStart(ball0, hole[0]);
                            StrategyHelper.Helpers.PoseToPose(ref decisions[1], fish2, desPoint, desRad0, 2f, 5f,
                                mission.CommonPara.MsPerCycle, ref times);
                        }
                    }
                }
            }
            #endregion

            #region 4号球 1号鱼
            if (b5 == 1 && b2 == 1 && b4 == 0 && (isMovingFlag4 == 0 || isMovingFlag4 == 1))
            {
                //游动到球右下方定点（+85,0,+85）
                if (t41 == false)
                {
                    int times = 0;
                    xna.Vector3 desPoint = new xna.Vector3(ball4.X + 85, 0, ball4.Z + 90);
                    StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, desPoint, -desRad4, 10f, 10f,
                        mission.CommonPara.MsPerCycle, ref times);
                    if (method.GetDistance(head1, ball4) > 200)
                    {
                        decisions[0].VCode += 1;
                    }
                }
                if (method.GetDistance(head1, hole[4]) - 70 > method.GetDistance(ball4, hole[4]) /*&& method.GetDistance(head1, ball4) < 120*/)
                {
                    t41 = true;
                }
                //调整至顶球点
                if (t41 == true)
                {
                    //开始调整至顶球点的时候，将正在搬运的标志设为1
                    isMovingFlag4 = 1;
                    int times = 0;
                    xna.Vector3 desPoint = method.GetPointOfStart(ball4, hole[4]);
                    StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, desPoint, desRad4, 2f, 5f,
                        mission.CommonPara.MsPerCycle, ref times);//阈值 改小一点
                    t42 = true;
                }
                //开始顶球
                if (t41 == true && t42 == true)
                {
                    if (method.GetDistance(head1, method.GetPointOfStart(ball4, hole[4])) < 20 && Math.Abs(xna.MathHelper.ToDegrees(fish1.BodyDirectionRad) - desRad4) < 25)
                    {
                        if (method.GetDistance(ball4, hole[4]) > 200)
                        {
                            StrategyHelper.Helpers.Dribble(ref decisions[0], fish1, hole[4], desRad4,
                                8f, 3f, 200f, 12, 4, 10, 100, false);
                        }
                        else
                        {
                            StrategyHelper.Helpers.Dribble(ref decisions[0], fish1, hole[4], desRad4,
                                4f, 3f, 20f, 8, 4, 8, 100, false);
                        }

                    }
                    else
                    {
                        //超过洞，快速调整
                        //if (((head1.X > hole[4].X && ball4.X < hole[4].X + 0)/* || (head1.X < hole[4].X && ball4.X > hole[4].X - 0)*/) && method.GetDistance(hole[4], head1) < 130)
                        //{
                        //    if (ball4.Z < hole[4].Z)
                        //    {
                        //        decisions[0].TCode = 0;
                        //        decisions[0].VCode += 1;
                        //   }
                        //    else
                        //    {
                        //        decisions[0].TCode = 14;
                        //        decisions[0].VCode += 1;
                        //    }
                        //}
                        //顶球过程中微调  
                        if (method.GetDistance(head1, hole[4]) + 10 < method.GetDistance(hole[4], ball4) && method.GetDistance(head1, ball4) < 120)
                        {
                            decisions[0].TCode = 7;
                            decisions[0].VCode = 13;
                        }
                        else
                        {
                            int times = 0;
                            xna.Vector3 desPoint = method.GetPointOfStart(ball4, hole[4]);
                            StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, desPoint, desRad4, 2f, 5f,
                                mission.CommonPara.MsPerCycle, ref times);
                        }
                    }
                }
            }
            #endregion


            //完成后离开
            //1号鱼
            if (b2 == 1 && b5 == 1)
            {
                //1号鱼离开的条件是：【1】搬运完5、2、4号球，0号球正在被2号鱼搬运；【2】搬运完5、2号球，4号球正在被2号鱼搬运
                if ((isMovingFlag0 == 2 && b4 == 1) || isMovingFlag4 == 2 || (b0 == 1 && b4 == 1 && isMovingFlag1 == 2) || (b0 == 1 && b4 == 1 && b1 == 1 && isMovingFlag3 == 2))
                {
                    decisions[0].VCode = 14;
                    decisions[0].TCode =7;
                }
                else if (isMovingFlag0 != 2 && b0 == 0 && b4 == 1)  //否则在0号球没进且4号球已进的条件下去搬运0号球
                {
                    #region 1号鱼搬运0号球
                    //游动到球右上方定点（+85,0,-90）
                    if (t01 == false)
                    {
                        int times = 0;
                        xna.Vector3 desPoint = new xna.Vector3(ball0.X + 85, 0, ball0.Z - 90);
                        StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, desPoint, -desRad0, 10f, 10f,
                            mission.CommonPara.MsPerCycle, ref times);
                        if (method.GetDistance(head1, ball0) > 200)
                        {
                            decisions[0].VCode += 1;
                        }
                    }
                    if (method.GetDistance(head1, hole[0]) - 70 > method.GetDistance(ball0, hole[0]) /*&& method.GetDistance(head1, ball0) < 120*/)
                    {
                        t01 = true;
                    }
                    //调整至顶球点
                    if (t01 == true)
                    {   //开始调整至顶球点的时候，将0号球正在搬运的标志设为1
                        isMovingFlag0 = 1;
                        int times = 0;
                        xna.Vector3 desPoint = method.GetPointOfStart(ball0, hole[0]);
                        StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, desPoint, desRad0, 2f, 5f,
                            mission.CommonPara.MsPerCycle, ref times);//阈值 改小一点
                        t02 = true;
                    }
                    //开始顶球
                    if (t01 == true && t02 == true)
                    {
                        if (method.GetDistance(head1, method.GetPointOfStart(ball0, hole[0])) < 20 && Math.Abs(xna.MathHelper.ToDegrees(fish1.BodyDirectionRad) - desRad0) < 25)
                        {
                            if (method.GetDistance(ball0, hole[0]) > 200)
                            {
                                StrategyHelper.Helpers.Dribble(ref decisions[0], fish1, hole[0], desRad0,
                                    8f, 3f, 200f, 12, 4, 10, 100, false);
                            }
                            else
                            {
                                StrategyHelper.Helpers.Dribble(ref decisions[0], fish1, hole[0], desRad0,
                                    4f, 3f, 20f, 8, 4, 3, 100, false);
                            }

                        }
                        else
                        {

                            //顶球过程中微调
                            if (method.GetDistance(head1, hole[0]) + 10 < method.GetDistance(hole[0], ball0) && method.GetDistance(head1, ball0) < 120)
                            {
                                decisions[0].TCode = 7;
                                decisions[0].VCode = 13;
                            }
                            else
                            {
                                int times = 0;
                                xna.Vector3 desPoint = method.GetPointOfStart(ball0, hole[0]);
                                StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, desPoint, desRad0, 2f, 5f,
                                    mission.CommonPara.MsPerCycle, ref times);
                            }
                        }
                    }

                    #endregion
                }
                else if (isMovingFlag1 != 2 && b0 == 1 && b4 == 1 && b1 == 0)
                {
                    #region 1号鱼搬运1号球
                    if (t11 == false)
                    {
                        int times = 0;
                        xna.Vector3 desPoint = new xna.Vector3(ball1.X + 85, 0, ball1.Z - 90);
                        StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, desPoint, -desRad1, 10f, 10f,
                            mission.CommonPara.MsPerCycle, ref times);
                        if (method.GetDistance(head1, ball1) > 200)
                        {
                            decisions[0].VCode += 1;
                        }
                    }
                    if (method.GetDistance(head1, hole[1]) - 70 > method.GetDistance(ball1, hole[1]) /*&& method.GetDistance(head2, ball1) < 120*/)
                    {
                        t11 = true;
                    }
                    //调整至顶球点
                    if (t11 == true)
                    {
                        isMovingFlag1 = 1;
                        int times = 0;
                        xna.Vector3 desPoint = method.GetPointOfStart(ball1, hole[1]);
                        StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, desPoint, desRad1, 2f, 5f,
                            mission.CommonPara.MsPerCycle, ref times);//阈值 改小一点
                        t12 = true;
                    }
                    //开始顶球
                    if (t11 == true && t12 == true)
                    {
                        if (method.GetDistance(head1, method.GetPointOfStart(ball1, hole[1])) < 20 && Math.Abs(xna.MathHelper.ToDegrees(fish1.BodyDirectionRad) - desRad1) < 25)
                        {
                            if (method.GetDistance(ball1, hole[1]) > 200)
                            {
                                StrategyHelper.Helpers.Dribble(ref decisions[0], fish1, hole[1], desRad1,
                                    8f, 3f, 200f, 12, 4, 10, 100, false);
                            }
                            else
                            {
                                StrategyHelper.Helpers.Dribble(ref decisions[0], fish1, hole[1], desRad1,
                                    4f, 3f, 20f, 8, 4, 3, 100, false);
                            }

                        }
                        else
                        {

                            //顶球过程中微调   
                            if (method.GetDistance(head1, hole[1]) + 10 < method.GetDistance(hole[1], ball1) && method.GetDistance(head1, ball1) < 120)
                            {
                                decisions[0].TCode = 7;
                                decisions[0].VCode = 13;
                            }
                            else
                            {
                                int times = 0;
                                xna.Vector3 desPoint = method.GetPointOfStart(ball1, hole[1]);
                                StrategyHelper.Helpers.PoseToPose(ref decisions[0], fish1, desPoint, desRad1, 2f, 5f,
                                    mission.CommonPara.MsPerCycle, ref times);
                            }
                        }
                    }
                }
                    #endregion
            }
            //2号鱼
            if (b1 == 1 && b3 == 1)
            {
                //2号鱼离开的条件是：【1】搬运完3、1、0号球，4号球正在被1号鱼搬运；【2】搬运完3、1号球，0号球正在被1号鱼搬运
                if ((isMovingFlag4 == 1 && b0 == 1) || isMovingFlag0 == 1 || (b0 == 1 && b4 == 1 && isMovingFlag2 == 1))
                {
                    decisions[1].VCode = 14;
                    decisions[1].TCode = 7;
                }
                else if (isMovingFlag4 != 1 && b4 == 0 && b0 == 1) //否则在4号没进且0号已进的条件下去搬运4号球
                {
                    #region 2号鱼，4号球
                    //游动到球右下方定点（+85,0,+85）
                    if (t41 == false)
                    {
                        int times = 0;
                        xna.Vector3 desPoint = new xna.Vector3(ball4.X + 85, 0, ball4.Z + 90);
                        StrategyHelper.Helpers.PoseToPose(ref decisions[1], fish2, desPoint, -desRad4, 10f, 10f,
                            mission.CommonPara.MsPerCycle, ref times);
                        if (method.GetDistance(head2, ball4) > 200)
                        {
                            decisions[1].VCode += 1;
                        }
                    }
                    if (method.GetDistance(head2, hole[4]) - 70 > method.GetDistance(ball4, hole[4]) /*&& method.GetDistance(head2, ball4) < 120*/)
                    {
                        t41 = true;
                    }
                    //调整至顶球点
                    if (t41 == true)
                    {
                        //开始调整至顶球点的时候，将正在搬运的标志设为2
                        isMovingFlag4 = 2;
                        int times = 0;
                        xna.Vector3 desPoint = method.GetPointOfStart(ball4, hole[4]);
                        StrategyHelper.Helpers.PoseToPose(ref decisions[1], fish2, desPoint, desRad4, 2f, 5f,
                            mission.CommonPara.MsPerCycle, ref times);//阈值 改小一点
                        t42 = true;
                    }
                    //开始顶球
                    if (t41 == true && t42 == true)
                    {
                        if (method.GetDistance(head2, method.GetPointOfStart(ball4, hole[4])) < 20 && Math.Abs(xna.MathHelper.ToDegrees(fish2.BodyDirectionRad) - desRad4) < 25)
                        {
                            if (method.GetDistance(ball4, hole[4]) > 200)
                            {
                                StrategyHelper.Helpers.Dribble(ref decisions[1], fish2, hole[4], desRad4,
                                    8f, 3f, 200f, 12, 4, 10, 100, false);
                            }
                            else
                            {
                                StrategyHelper.Helpers.Dribble(ref decisions[1], fish2, hole[4], desRad4,
                                    4f, 3f, 20f, 8, 4, 3, 100, false);
                            }

                        }
                        else
                        {
                            //顶球过程中微调  
                            if (method.GetDistance(head2, hole[4]) + 10 < method.GetDistance(hole[4], ball4) && method.GetDistance(head2, ball4) < 120)
                            {
                                decisions[1].TCode = 7;
                                decisions[1].VCode = 13;
                            }
                            else
                            {
                                int times = 0;
                                xna.Vector3 desPoint = method.GetPointOfStart(ball4, hole[4]);
                                StrategyHelper.Helpers.PoseToPose(ref decisions[1], fish2, desPoint, desRad4, 2f, 5f,
                                    mission.CommonPara.MsPerCycle, ref times);
                            }
                        }
                    }
                    #endregion
                }
                else if (isMovingFlag2 != 1 && b0 == 1 && b4 == 1 && b2 == 0)
                {
                    if (t21 == false)
                    {
                        int times = 0;
                        xna.Vector3 desPoint = new xna.Vector3(ball2.X + 85, 0, ball2.Z + 90);
                        StrategyHelper.Helpers.PoseToPose(ref decisions[1], fish2, desPoint, -desRad2, 10f, 10f,
                            mission.CommonPara.MsPerCycle, ref times);
                        if (method.GetDistance(head2, ball2) > 200)
                        {
                            decisions[1].VCode += 1;
                        }
                    }
                    //游动到球右下方定点（+85,0,+85）
                    if (t21 == false)
                    {
                        int times = 0;
                        xna.Vector3 desPoint = new xna.Vector3(ball2.X + 85, 0, ball2.Z + 90);
                        StrategyHelper.Helpers.PoseToPose(ref decisions[1], fish2, desPoint, -desRad2, 10f, 10f,
                            mission.CommonPara.MsPerCycle, ref times);
                        if (method.GetDistance(head1, ball2) > 200)
                        {
                            decisions[1].VCode += 3;
                        }
                    }
                    if (method.GetDistance(head2, hole[2]) - 70 > method.GetDistance(ball2, hole[2])/*&& method.GetDistance(head1, ball2) < 120*/)
                    {
                        t21 = true;
                    }
                    //调整至顶球点
                    if (t21 == true)
                    {
                        isMovingFlag2 = 2;
                        int times = 0;
                        xna.Vector3 desPoint = method.GetPointOfStart(ball2, hole[2]);
                        StrategyHelper.Helpers.PoseToPose(ref decisions[1], fish2, desPoint, desRad2, 2f, 5f,
                            mission.CommonPara.MsPerCycle, ref times);//阈值 改小一点
                        t22 = true;
                    }
                    //开始顶球
                    if (t21 == true && t22 == true)
                    {
                        if (method.GetDistance(head2, method.GetPointOfStart(ball2, hole[2])) < 20 && Math.Abs(xna.MathHelper.ToDegrees(fish2.BodyDirectionRad) - desRad2) < 25)
                        {
                            if (method.GetDistance(ball2, hole[2]) > 200)
                            {
                                StrategyHelper.Helpers.Dribble(ref decisions[1], fish2, hole[2], desRad2,
                                    8f, 3f, 200f, 12, 4, 10, 100, false);
                            }
                            else
                            {
                                StrategyHelper.Helpers.Dribble(ref decisions[1], fish2, hole[2], desRad2,
                                    4f, 3f, 20f, 8, 4, 3, 100, false);
                            }

                        }
                        else
                        {

                            //顶球过程中微调   
                            if (method.GetDistance(head2, hole[2]) + 10 < method.GetDistance(hole[2], ball2) && method.GetDistance(head2, ball2) < 120)
                            {
                                decisions[1].TCode = 7;
                                decisions[1].VCode = 13;
                            }
                            else
                            {
                                int times = 0;
                                xna.Vector3 desPoint = method.GetPointOfStart(ball2, hole[2]);
                                StrategyHelper.Helpers.PoseToPose(ref decisions[1], fish2, desPoint, desRad2, 2f, 5f,
                                    mission.CommonPara.MsPerCycle, ref times);
                            }
                        }
                    }
                }

            }
            #endregion
            return decisions;
        }
    }
}
