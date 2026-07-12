# FR5 Workcell Model Manifest

| Category | Asset Name | Manufacturer | Intended Role | Formats | Notes |
|---|---|---|---|---|---|
| Conveyor | BIFA BFZY01 Curve90 | BIFA | 90-degree curved powered conveyor | FBX, STP | R600, W500, H700 workcell conveyor section |
| Conveyor | BIFA BFZY02 Curve180 | BIFA | 180-degree curved powered conveyor | FBX, STP | R600, W500, H700 workcell conveyor section |
| Conveyor | BIFA PoweredRoller Straight | BIFA | Straight powered roller conveyor | FBX, STP | L1500, W500, H700 workcell conveyor section |
| RobotTable | Rittal AX Robot Cabinet | Rittal | FR5 robot controller cabinet and support base | FBX, STP | Virtual workcell robot table/cabinet representation |
| RobotTable | Blickle Levelling Caster | Blickle | Cabinet levelling caster | FBX, STP | Nominal 100 mm caster model |
| RobotTable | Grid Base Plate | Unspecified | Robot/cabinet mounting base plate | FBX, STP | Grid mounting plate for virtual layout |
| Safety | Robotunits Safety Fence | Robotunits | Perimeter guarding | FBX, STP | Allround panel, W1034, H2021 |
| Safety | Robotunits Safety Door | Robotunits | Guarded operator access | FBX, STP | Single door, W1124, H2200 |
| Safety | Safety Light Curtain | Unspecified | Safety access detection | STP | Receiver/body source model |
| Safety | Safety Light Curtain Emitter | Unspecified | Safety light curtain emitter | FBX | Emitter source model |
| Safety | Emergency Stop Button | Unspecified | Emergency stop operator control | FBX, STP | Visual model only; safety logic remains separate |
| Vision | Balluff BVS CA-GX0 Camera | Balluff | Machine-vision camera | FBX, STP | Workcell inspection camera representation |
| Control | HMI Operator Panel | Unspecified | Operator interface panel | FBX, STP | Visual HMI enclosure/panel model |
| Control | IFM DV1520 Stack Light | ifm | Workcell status indication | FBX, STP | Multi-segment signal light representation |
| Fixture | norelem Fixture Plate | norelem | Workpiece fixture base | FBX, STP | 01140-2102X400 fixture plate model |
| Sensors | Conveyor Entry Sensor | Unspecified | Conveyor infeed detection | FBX, STP | Logical sensing behavior remains separate |
| Workpieces | Base Housing | Unspecified | Primary process workpiece | FBX, STP | Base housing source geometry |
| Workpieces | Sensor Module | Unspecified | Process workpiece/module | FBX, STP | Compared with the Pepperl+Fuchs sensor; retained as a separate asset |
| Workpieces | Pepperl+Fuchs NBN8 Sensor | Pepperl+Fuchs | Inductive proximity sensor/workpiece component | FBX, STP | NBN8-18GM50-E2-V1 source model; retained separately |
| Storage | Robotunits BOX4030 Tray | Robotunits | Workpiece storage tray | FBX, STP | 400 x 300 tray representation |
| Profiles | Robotunits PIL5050 Profile | Robotunits | Structural framing profile | FBX, STP | L1500 profile source model |

## Usage Notes

- 모델 치수는 실제 하드웨어 제작 기준이 아니라 Unity 가상 공정 시각화용이다.
- 최종 Unity 배치에서는 1 Unity Unit = 1 meter 기준으로 스케일을 보정한다.
- 기능용 Collider, Conveyor Path, Pick Point는 모델과 별도 GameObject로 구성한다.
