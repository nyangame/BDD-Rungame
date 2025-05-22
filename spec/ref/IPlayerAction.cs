enum ActionTagType {
  None,
  FreeMoveAction,
  BlockingAction,
}

enum InputType {
  None,
  MoveStick,
  MoveAxis
  OK, //Attack
  NG, //Jump
  Slide,
  Pause
}

interface IPlayerAction {
  ActionTagType ActionTag { get; }
  bool IsExit();
  void Enter();
  void Update();
  void Input(InputType key);
}
