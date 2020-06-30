using System;
using xna = Microsoft.Xna.Framework;
using URWPGSim2D.Common;
using URWPGSim2D.StrategyLoader;


namespace URWPGSim2D.Strategy
{
    public static class method
    {
        public static void swimToDES(float x, float z, float ballx, float ballz, float detax, float detaz, ref Decision[] decisions, int fishID, Mission mission, int teamId, ref bool t)
        {
            int times = 0;//记录pose2pose算法进入第二阶段的时间
            xna.Vector3 v1 = new xna.Vector3(ballx + detax - x, 0, ballz + detaz - z);
            float fishrad1 = StrategyHelper.Helpers.GetAngleDegree(v1);//目标方向角度经验值，具体使用需要根据数学方法计算
            xna.Vector3 dright1 = new xna.Vector3(ballx + detax, 0f, ballz + detaz); //目标点的坐标
            StrategyHelper.Helpers.PoseToPose(ref decisions[fishID], mission.TeamsRef[teamId].Fishes[fishID],
            dright1, fishrad1, 20f, 30f, mission.CommonPara.MsPerCycle, ref times);
            t = true;
        }
        public static void adjust(float x, float z, float ballx, float ballz, ref Decision[] decisions, Mission mission, int fishID, int teamId, ref bool t, float deta_x, float deta_z)
        {
            int times = 0;//记录pose2pose算法进入第二阶段的时间
            xna.Vector3 dright = new xna.Vector3(ballx + deta_x, 0f, ballz + deta_z);             //5
            xna.Vector3 dright1 = new xna.Vector3(x, 0f, z);//(bx + 240, 0f, bz + 168);
            xna.Vector3 v1 = (dright - dright1);
            float fishrad = StrategyHelper.Helpers.GetAngleDegree(v1);
            StrategyHelper.Helpers.PoseToPose(ref decisions[fishID], mission.TeamsRef[teamId].Fishes[fishID],
            dright, fishrad, 15f, 50f, mission.CommonPara.MsPerCycle, ref times);
            t = true;
        }
        #region 返回鱼到达定点需要转的角度
        public static float Getxzdangle(float cur_x, float cur_z, float dest_x, float dest_z, float fish_rad)
        {
            //鱼体方 向mission.TeamsRef[teamId].Fishes[i].BodyDirectionRad
            float curangle;
            float xzdangle = fish_rad;
            curangle = (float)(Math.Abs(Math.Atan((cur_x - dest_x) / (cur_z - dest_z))));//弧度制
            if ((cur_x > dest_x) && (cur_z > dest_z))
            {//以球为中心，当鱼在球的右下方
                if (fish_rad < 0 && fish_rad > -Math.PI / 2)
                {
                    xzdangle = -(float)(Math.PI / 2 + curangle + fish_rad);
                }
                else if (fish_rad > (-Math.PI) && fish_rad < -(Math.PI / 2))
                {
                    xzdangle = (float)(-Math.PI / 2 - fish_rad - curangle);
                }

                else
                    if (1.5 * Math.PI - fish_rad - curangle < fish_rad + 0.5 * Math.PI)
                        xzdangle = (float)(Math.PI * 1.5 - fish_rad - curangle);
                    else
                        xzdangle = (float)(fish_rad + 0.5 * Math.PI);
            }
            else if ((cur_x > dest_x) && (cur_z < dest_z))
            {//以球为中心，当鱼在球的右上方
                if (fish_rad < ((Math.PI / 2 + curangle)) && (-(Math.PI / 2 - curangle)) < fish_rad)
                {
                    xzdangle = (float)(Math.PI / 2 + curangle - fish_rad);
                }
                else if ((-(Math.PI / 2 - curangle) > fish_rad) && fish_rad > -(Math.PI))
                {
                    xzdangle = (float)(Math.PI * 2 + fish_rad - curangle);
                    xzdangle = -xzdangle;
                }
                else if (fish_rad > ((Math.PI / 2 + curangle)) && fish_rad < (Math.PI))
                {
                    xzdangle = (float)(fish_rad - Math.PI / 2 - curangle);
                    xzdangle = -xzdangle;
                }
            }
            else if ((cur_x < dest_x) && (cur_z < dest_z))
            {//以球为中心，当鱼在球的左上方
                if (fish_rad >= 0 && fish_rad < Math.PI)
                {
                    xzdangle = (float)(curangle - fish_rad);

                }
                else if (fish_rad > 0.5 * Math.PI && fish_rad < Math.PI)
                {
                    xzdangle = (float)(fish_rad - curangle);

                }
                else
                {
                    if (-fish_rad + curangle > 2 * Math.PI + fish_rad - curangle)
                        xzdangle = -(float)(2 * Math.PI + fish_rad - curangle);
                    else
                        xzdangle = (float)(-fish_rad + curangle);
                }

            }
            else if ((cur_x < dest_x) && (cur_z > dest_z))
            {//以球为中心，当鱼在球的左下方
                if (fish_rad >= 0 && fish_rad <= Math.PI)
                {
                    if (curangle + fish_rad < Math.PI * 2 - curangle - fish_rad)
                        xzdangle = -(float)(curangle + fish_rad);
                    else
                        xzdangle = (float)(Math.PI * 2 - curangle - fish_rad);
                }

                else
                {
                    if (fish_rad > -Math.PI && fish_rad < -0.5 * Math.PI)
                        xzdangle = (float)-(fish_rad + curangle);
                    else
                        xzdangle = (float)(fish_rad + curangle);
                }
            }
            return xzdangle;
        }
        #endregion
        #region 获得角度
        public static double angel(RoboFish fish, xna.Vector3 destPtMm)
        {
            xna.Vector3 srcPtMm = fish.PositionMm;
            // 起始点到目标点的距离（目标距离）
            double dirFishToDestPtRad = xna.MathHelper.ToRadians((float)StrategyHelper.Helpers.GetAngleDegree(destPtMm - fish.PositionMm));

            // 中间方向与鱼体方向的差值（目标角度）
            double deltaTheta = dirFishToDestPtRad - fish.BodyDirectionRad;
            // 将目标角度规范化到(-PI,PI]
            // 规范化之后目标角度为正，表示目标方向在鱼体方向右边
            // 规范化之后目标角度为负，表示目标方向在鱼体方向左边
            if (deltaTheta > Math.PI)
            {// 中间方向为正鱼体方向为负才可能目标角度大于PI
                deltaTheta -= 2 * Math.PI;  // 规范化到(-PI,0)
            }
            else if (deltaTheta < -Math.PI)
            {// 中间方向为负鱼体方向为正才可能目标角度小于-PI
                deltaTheta += 2 * Math.PI;  // 规范化到(0,PI)
            }
            return deltaTheta;
        }
        #endregion
        #region 将角速度值转换为所需的角速度档位
        public static int TransfromAngletoTCode(float angvel)
        {
            if (angvel == 0)
            {
                return 7;
            }
            else if (angvel < 0)
            {
                if (-0.005395 <= angvel && 0 > angvel)
                {
                    if ((0 - angvel) >= (angvel + 0.005395))
                    {
                        return 6;
                    }
                    else
                    {
                        return 7;
                    }
                }
                else if (-0.009016 <= angvel && -0.005395 > angvel)
                {
                    if ((-0.005395 - angvel) >= (angvel + 0.009016))
                    {
                        return 5;
                    }
                    else
                    {
                        return 6;
                    }
                }
                else if (-0.014203 <= angvel && 0.009016 > angvel)
                {
                    if ((-0.009016 - angvel) >= (angvel + 0.014203))
                    {
                        return 4;
                    }
                    else
                    {
                        return 5;
                    }
                }
                else if (-0.019907 <= angvel && -0.014203 > angvel)
                {
                    if ((-0.014203 - angvel) >= (angvel + 0.019907))
                    {
                        return 3;
                    }
                    else
                    {
                        return 4;
                    }
                }
                else if (-0.0253 <= angvel && -0.019907 > angvel)
                {
                    if ((-0.019907 - angvel) >= (angvel + 0.0253))
                    {
                        return 2;
                    }
                    else
                    {
                        return 3;
                    }
                }
                else if (-0.033592 <= angvel && -0.0253 > angvel)
                {
                    if ((-0.0253 - angvel) >= (angvel + 0.033592))
                    {
                        return 1;
                    }
                    else
                    {
                        return 2;
                    }
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                if (0.005395 >= angvel && 0 < angvel)
                {
                    if (angvel - 0 > 0.005395 - angvel)
                    {
                        return 8;
                    }
                    else
                    {
                        return 7;
                    }
                }
                else if (0.009016 >= angvel && 0.005395 < angvel)
                {
                    if (angvel - 0.005395 > 0.009016 - angvel)
                        return 9;
                    else
                        return 8;
                }
                else if (0.014203 >= angvel && 0.009016 < angvel)
                {
                    if (angvel - 0.009016 > 0.014203 - angvel)
                        return 10;
                    else
                        return 9;
                }
                else if (0.019907 >= angvel && 0.014203 < angvel)
                {
                    if (angvel - 0.014203 > 0.019907 - angvel)
                        return 11;
                    else
                        return 10;
                }
                else if (0.0253 >= angvel && 0.019907 < angvel)
                {
                    if (angvel - 0.019907 > 0.0253 - angvel)
                        return 12;
                    else
                        return 11;
                }
                else if (0.033592 >= angvel && 0.0253 < angvel)
                {
                    if (angvel - 0.0253 > 0.033592 - angvel)
                        return 13;
                    else
                        return 12;
                }
                else if (0.040848 >= angvel && 0.033592 < angvel)
                {
                    if (angvel - 0.033592 > 0.040848 - angvel)
                        return 14;
                    else
                        return 13;
                }
                else
                    return 14;

            }
        }
        #endregion
        #region 得到离目标500位置x坐标
        public static double des_x(float cur_x, float cur_z, float dest_x, float dest_z)
        {
            double x1 = Math.Abs(cur_x - dest_x);
            double y1 = Math.Abs(cur_z - dest_z);
            double y = Math.Sqrt(x1 * x1 + y1 * y1);
            double L = Math.Abs(Math.Sqrt(x1 * x1 + y1 * y1) - 500);
            double x = x1 * L / y + cur_x;
            return x;
        }
        #endregion
        #region 得到离目标500位置z坐标
        public static double des_z(double cur_x, double cur_z, double dest_x, double dest_z)
        {
            double x1 = Math.Abs(cur_x - dest_x);
            double y1 = Math.Abs(cur_z - dest_z);
            double y = Math.Sqrt(x1 * x1 + y1 * y1);
            double L = Math.Abs(Math.Sqrt(x1 * x1 + y1 * y1) - 500);
            double z = (y1 * L / y) + cur_z;
            return z;
        }
        #endregion
        #region
        public static float dest_x(float fish_rad, float dest_x)
        {

            return (float)(dest_x + 500 * Math.Cos(fish_rad));
        }
        #endregion
        #region
        public static float dest_z(float fish_rad, float dest_z)
        {

            return (float)(dest_z + 500 * Math.Cos(fish_rad));
        }
        #endregion
        public static xna.Vector3 getAjustDesPoint(float desRad, float ballx, float ballz)
        {
            float rad = Math.Abs(xna.MathHelper.ToRadians(desRad));
            float x, z;
            if (desRad > 0)
            {
                x = ballx + 200 * Math.Abs((float)Math.Cos(rad));
                z = ballz + 200 * Math.Abs((float)Math.Sin(rad));
            }
            else
            {
                x = ballx + 200 * Math.Abs((float)Math.Cos(rad));
                z = ballz - 200 * Math.Abs((float)Math.Sin(rad));
            }
            return new xna.Vector3(x, 0, z);
        }

        #region 计算顶球点
        public static xna.Vector3 GetPointOfStart(xna.Vector3 ball, xna.Vector3 hole)
        {
            xna.Vector3 point = new xna.Vector3(0, 0, 0);

            if (ball.X > hole.X && ball.Z > hole.Z)//球在洞的右下角
            {
                float degree = StrategyHelper.Helpers.GetAngleDegree(hole - ball);
                float rad = xna.MathHelper.ToRadians(degree);
                rad = (float)Math.PI + rad;
                point = new xna.Vector3(ball.X + 58 * (float)Math.Cos(rad), 0, ball.Z + 58 * (float)Math.Sin(rad));
            }
            if (ball.X > hole.X && ball.Z < hole.Z)//秋在洞的右上角
            {
                float degree = StrategyHelper.Helpers.GetAngleDegree(hole - ball);
                float rad = xna.MathHelper.ToRadians(degree);
                rad = (float)Math.PI - rad;
                point = new xna.Vector3(ball.X + 58 * (float)Math.Cos(rad), 0, ball.Z - 58 * (float)Math.Sin(rad));
            }
            if (ball.X < hole.X && ball.Z > hole.Z)//秋在洞的左下角
            {
                float degree = StrategyHelper.Helpers.GetAngleDegree(hole - ball);
                float rad = xna.MathHelper.ToRadians(degree);
                rad = -rad;
                point = new xna.Vector3(ball.X - 58 * (float)Math.Cos(rad), 0, ball.Z + 58 * (float)Math.Sin(rad));
            }
            if (ball.X < hole.X && ball.Z < hole.Z)//秋在洞的左上角
            {
                float degree = StrategyHelper.Helpers.GetAngleDegree(hole - ball);
                float rad = xna.MathHelper.ToRadians(degree);
                point = new xna.Vector3(ball.X - 58 * (float)Math.Cos(rad), 0, ball.Z - 58 * (float)Math.Sin(rad));
            }

            return point;
        }
        #endregion

        #region 计算两点间的距离
        public static float GetDistance(xna.Vector3 A, xna.Vector3 B)
        {
            return (float)Math.Sqrt(Math.Pow(A.X - B.X, 2.0) + Math.Pow(A.Z - B.Z, 2.0));
        }

        #endregion
        #region 通过转动解决鱼无限推球解法
        /*算法1，使用角度，情况复杂，且不易实现
        /// <summary>
        /// </summary>
        /// <param name="aimAngle">球->洞的向量角度</param>
        /// <param name="fishAngle">鱼的方向</param>
        /// /// <param name="decision">对象鱼</param>
        public static void swimAway(float aimAngle, float fishAngle, ref Decision decision)
        {
            int vcode = 7;
            float dangle = aimAngle - fishAngle;
            if (aimAngle >= -135 && aimAngle <= 135)
            {
                if (dangle > 180 && dangle < 225)
                {
                    decision.VCode = vcode;
                    decision.TCode = 0;
                }
                else if (dangle > 135 && dangle < 180)
                {
                    decision.VCode = vcode;
                    decision.TCode = 14;
                }
                else if (dangle > -180 && dangle < -135)
                {
                    decision.VCode = vcode;
                    decision.TCode = 0;
                }
                else if (dangle > -225 && dangle < -180)
                {
                    decision.VCode = vcode;
                    decision.TCode = 14;
                }
            }
            else if (aimAngle > -180 && aimAngle < -135)
            {
                if (dangle > 135 && dangle < 180)
                {
                    decision.VCode = vcode;
                    decision.TCode = 14;
                }
                else if (dangle > 180 && dangle < 225)
                {
                    decision.VCode = vcode;
                    decision.TCode = 0;
                }
                else if (dangle > 135 && dangle < 180)
                {
                    decision.VCode = vcode;
                    decision.TCode = 0;
                }
            }
            else if (aimAngle > 135 && aimAngle < 180)
            {
                if (dangle > -180 && dangle < -135)
                {
                    decision.VCode = vcode;
                    decision.TCode = 0;
                }
                else if (dangle > -225 && dangle < -180)
                {
                    decision.VCode = vcode;
                    decision.TCode = 14;
                }
                else if (dangle > -180 && dangle < -135)
                {
                    decision.VCode = vcode;
                    decision.TCode = 14;
                }
            }
        }*/
        /*算法2，无法达到预期效果
         * /// <summary>
        /// </summary>
        /// <param name="decision">决策变量 输出参数 会被修改</param>
        /// <param name="fish">机器鱼对象</param>
        /// <param name="aimAngle">球->洞向量方向</param>
        /// <param name="hole">洞坐标</param>
        /// <param name="ball">球坐标</param>
        public static void swimAway(ref Decision decision, RoboFish fish, float aimAngle, xna.Vector3 hole, xna.Vector3 ball)
        {
            float limangle=60;//角度阈值设定值
            int vcode = 14;//速度档位设定值
            float a = getDistance(ball, hole);
            float b = getDistance(fish.PositionMm, fish.PolygonVertices[0]);
            xna.Vector3 dFishhead = new xna.Vector3(fish.PolygonVertices[0].X + ball.X - fish.PositionMm.X, 0, fish.PrePolygonVertices[0].Z + ball.Z - fish.PositionMm.Z);
            float c = getDistance(dFishhead, hole);
            float angle = Math.Abs((float)Math.Acos((a * a + b * b - c * c) / (2 * a * b)));
            if (angle >= limangle)
            {
                xna.Vector3 direction1 = ball - fish.PositionMm;
                xna.Vector3 direction2 = fish.PolygonVertices[0] - fish.PositionMm;
                float angle1 = StrategyHelper.Helpers.GetAngleDegree(direction1);//鱼体中心与球的向量
                float angle2 = StrategyHelper.Helpers.GetAngleDegree(direction2);//鱼的方向
                if (angle1 > 90 && angle1 < 180 && angle2 < -90 && angle2 > -180)
                {
                    decision.VCode = vcode;
                    decision.TCode = 7;
                }
                else if (angle2 > 90 && angle2 < 180 && angle1 < -90 && angle1 > -180)
                {
                    decision.VCode = vcode;
                    decision.TCode = 0;
                }
                    if (angle1 >= angle2)
                    {
                        decision.VCode = vcode;
                        decision.TCode = 0;
                    }
                    else if (angle1 < angle2)
                    {
                        decision.VCode = vcode;
                        decision.TCode = 14;
                    }
            }
        }*/
        //算法3，使用一段posetoopose去一个临时点
        #endregion

        public static float Getxzdangle(RoboFish fish, xna.Vector3 aimPosition)
        {
            xna.Vector3 aimVector;
            aimVector.X = aimPosition.X - fish.PolygonVertices[0].X;
            aimVector.Z = aimPosition.Z - fish.PolygonVertices[0].Z;
            aimVector.Y = 0;
            //float aimAngle = StrategyHelper.Helpers.GetAngleDegree(aimVector);
            xna.Vector3 fishRad = new xna.Vector3((float)Math.Cos(fish.BodyDirectionRad), 0, (float)Math.Sin(fish.BodyDirectionRad));
            //公式：θ=atan2(v2.y,v2.x)?atan2(v1.y,v1.x)
            //atan2的取值范围是[?π,π]，在进行相减之后得到的夹角是在[?2π,2π]，
            //因此当得到的结果大于π时，对结果减去2π，当结果小于?π时，对结果加上2π
            //虽然与一般坐标方向不一致，但是象限都是3 4 1 2的顺序，所以仍然成立
            //但是仍需验证
            float theta = (float)Math.Atan2(aimVector.Z, aimVector.X) - (float)Math.Atan2(fishRad.Z, fishRad.X);
            if (theta > Math.PI)
                theta -= (float)(2 * Math.PI);
            else if (theta < -Math.PI)
                theta += (float)(2 * Math.PI);
            return theta;
        }

        private static float[] Vtable = { 0, 9.0152f, 31.5533f, 60.4020f, 88.3492f, 110.6591f, 132.7829f, 152.1562f, 172.9048f, 204.6457f, 268.5165f, 289.3336f, 295.6592f, 293.9903f, 303.6920f };
        private static float[] Ttable = { -0.3552f, -0.2921f, -0.2200f, -0.1731f, -0.1235f, -0.0784f, -0.0469f, 0, 0.0469f, 0.0784f, 0.1235f, 0.1731f, 0.2200f, 0.2921f, 0.3438f };
        public static float getVtable(int t)
        {
            return Vtable[t];
        }
        public static float getTtable(int t)
        {
            return Ttable[t];
        }
        #region 鱼体简单快速运动封装 控制鱼到达某点（最常用运动函数）
        /// <summary>
        /// 鱼体简单快速运动封装
        /// </summary>
        /// <param name="decision">鱼的决策对象</param>
        /// <param name="fish">鱼的参数只读对象</param>
        /// <param name="aim_point">目标位置</param>
        /// <param name="Vcode1">巡航速度最大值,可以取14，实际档位由程序决定</param>
        /// <param name="Vcode2">减速第一阶段，默认为8</param>
        /// <param name="Vcode3">减速第二阶段，默认为6</param>
        static public void approachToPoint(ref Decision decision, RoboFish fish, xna.Vector3 aim_point, int Vcode1, int Vcode2, int Vcode3, int i)
        {
            float angle = Getxzdangle(fish, aim_point);
            int Tcode = GetxzdTcode(angle, i);
            decision.TCode = Tcode;
            float distance = GetDistance(fish.PolygonVertices[0], aim_point);
            if (distance > 200)
            {
                int autoVcode = GetxzdVcode(fish, aim_point);
                decision.VCode = Vcode1 < autoVcode ? Vcode1 : autoVcode;//较大距离时巡航速度
            }
            else if (distance >= 100)
            {
                decision.VCode = Vcode2;//第一阶段减速
            }
            else
            {
                decision.VCode = Vcode3;//第二阶段减速
            }
        }
        #endregion
        public static int GetxzdTcode(float angvel, int i)
        {
            //float interval = 1f / 7;//每个划分区间的宽度
            //if (angvel == 0)
            //    return 7;

            //else if (angvel < interval * Math.PI && angvel > 0)
            //    return 8;
            //else if (angvel < 2 * interval * Math.PI && angvel >= interval * Math.PI)
            //    return 9;
            //else if (angvel < 3 * interval * Math.PI && angvel >= 2 * interval * Math.PI)
            //    return 10;
            //else if (angvel < 4 * interval * Math.PI && angvel >= 3 * interval * Math.PI)
            //    return 11;
            //else if (angvel < 5 * interval * Math.PI && angvel >= 4 * interval * Math.PI)
            //    return 12;
            //else if (angvel < 6 * interval * Math.PI && angvel >= 5 * interval * Math.PI)
            //    return 13;
            //else if (angvel <= Math.PI && angvel >= 5 * interval * Math.PI)
            //    return 14;


            //else if (-angvel < interval * Math.PI && -angvel > 0)
            //    return 6;
            //else if (-angvel < 2 * interval * Math.PI && -angvel >= interval * Math.PI)
            //    return 5;
            //else if (-angvel < 3 * interval * Math.PI && -angvel >= 2 * interval * Math.PI)
            //    return 4;
            //else if (-angvel < 4 * interval * Math.PI && -angvel >= 3 * interval * Math.PI)
            //    return 3;
            //else if (-angvel < 5 * interval * Math.PI && -angvel >= 4 * interval * Math.PI)
            //    return 2;
            //else if (-angvel < 6 * interval * Math.PI && -angvel >= 5 * interval * Math.PI)
            //    return 1;
            //else if (-angvel <= Math.PI && -angvel >= 5 * interval * Math.PI)
            //    return 0;

            //else return 7;


            if (angvel < 0) return (7 - i);
            else if (angvel > 0) return (7 + i);
            else return 7;

        }

        #region 鱼转弯速度推荐决策值



        public static int GetxzdVcode(RoboFish fish, xna.Vector3 destpoint)
        {
            float rad = Getxzdangle(fish, destpoint);
            float t = Math.Abs(rad / getTtable(0));//最大转弯幅度预计时间
            float dis = GetDistance(fish.PolygonVertices[0], destpoint);
            for (int i = 0; i <= 14; i++)
            {
                if (dis / t <= getVtable(i))
                {
                    return i;
                }
            }
            return 14;
        }
        public static int _GetxzdVcode(float angvel)
        {
            float interval = 0.2f;//每个划分区间的宽度
            if (angvel <= interval * Math.PI && angvel >= -interval * Math.PI)
                return 14;
            else if (angvel <= 2 * interval * Math.PI && angvel >= -2 * interval * Math.PI)
                return 11;
            else if (angvel <= 3 * interval * Math.PI && angvel >= -3 * interval * Math.PI)
                return 8;
            else if (angvel <= 4 * interval * Math.PI && angvel >= -4 * interval * Math.PI)
                return 5;
            else return 2;

        }
        #region 将向量换算为角度
        public static float GetRadByVector(xna.Vector3 vec)
        {
            float rad = (float)Math.Atan2(vec.Z, vec.X);
            return rad;
        }
        #region 顶球点和鱼的位置冲突，无法直线到达，防止无限顶球
        /// <summary>
        /// 顶球点和鱼的位置冲突，无法直线到达，防止无限顶球
        /// </summary>
        /// <param name="decision">决策变量</param>
        /// <param name="fish">鱼的只读属性</param>
        /// <param name="ball">球的坐标</param>
        /// <param name="point">顶球点</param>
        /// <returns>是否进行了调整</returns>
        public static xna.Vector3 go_aside(ref Decision decision, RoboFish fish, xna.Vector3 ball, xna.Vector3 point)
        {
            //获取顶球点的对称点
            xna.Vector3 point2 = new xna.Vector3(ball.X + (ball.X - point.X), 0, ball.Z + (ball.Z - point.Z));
            if (GetDistance(fish.PolygonVertices[0], point) > 18.7f)
            {
                /* xna.Vector3 vector_point = new xna.Vector3(point.X - ball.X, 0, point.Z - ball.Z);//从球到顶球点的向量
                 float rad_point = GetRadByVector(vector_point);
                 float angle = rad_point - fish.BodyDirectionRad;
                 if (angle > Math.PI) angle -= (float)(2 * Math.PI);
                 else if (angle < -Math.PI) angle += (float)(2 * Math.PI);
                 if (Math.Abs(angle) < (float)(0.2 * Math.PI))
                 {
                     //获得球的边缘上另外两点，以判断鱼头位置而调整方向
                     //计算鱼头和顶球点的中点坐标
                     xna.Vector3 mid = new xna.Vector3( 0.5f*(fish.PolygonVertices[0].X  + point.X), 0, 0.5f * (fish.PolygonVertices[0].Z + point.Z));
                     //计算鱼头和顶球点中垂线上一临时点M
                     xna.Vector3 M = new xna.Vector3((float)(5.0 * mid.X - 4.0 * ball.X), 0, (float)(5.0 * mid.Z - 4.0 * ball.Z));
                     //decision.VCode = 14;
                     return M;//表示需要调整
                 }*/
                //计算鱼头和顶球点的中点坐标

                xna.Vector3 mid = new xna.Vector3(0.5f * (fish.PolygonVertices[0].X + point.X), 0, 0.5f * (fish.PolygonVertices[0].Z + point.Z));
                //计算鱼头和顶球点中垂线上一临时点M
                if (mid.X == ball.X && mid.Z == ball.Z)//当顶球点和鱼头连线过球心时
                {
                    xna.Vector3 shuiping = new xna.Vector3(1400 - ball.X, 0, 0);//从球心往右的水平线向量
                    xna.Vector3 despoint = new xna.Vector3(point.X - fish.PolygonVertices[0].X, 0, point.Z - fish.PolygonVertices[0].Z);//从鱼头到顶球点的向量
                    float rad_point = GetRadByVector(despoint);
                    float rad = 0f;
                    if (rad_point > 0.5 * Math.PI || rad_point < -0.5 * Math.PI)
                    {
                        rad = (float)Math.Abs((Math.PI - rad_point));
                    }
                    else
                    {
                        rad = (float)Math.Abs(rad_point);
                    }
                    xna.Vector3 M = new xna.Vector3((float)(ball.X - 3 * 58 * Math.Sin(rad)), 0, (float)(ball.Z + 3 * 58 * Math.Cos(rad)));
                    return M;
                }
                else
                {
                    xna.Vector3 M = new xna.Vector3((float)(5.0 * mid.X - 4.0 * ball.X), 0, (float)(5.0 * mid.Z - 4.0 * ball.Z));
                    return M;
                }

            }
            return point;
        }
        #endregion



    }

        #endregion
}
        #endregion
