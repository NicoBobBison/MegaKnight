using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ChessBot.GUI
{
    internal static class InputManager
    {
        static MouseState _currentMouse;
        static MouseState _prevMouse;
        public static void Update(GameTime gameTime)
        {
            _prevMouse = _currentMouse;
            _currentMouse = Mouse.GetState();
        }
        public static bool GetMouseDown()
        {
            return _currentMouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
        }
        public static Vector2 GetMousePosition()
        {
            return new Vector2(_currentMouse.X, _currentMouse.Y);
        }
        public static bool IsHovering(Rectangle rect)
        {
            return rect.Contains(_currentMouse.Position);
        }
    }
}
