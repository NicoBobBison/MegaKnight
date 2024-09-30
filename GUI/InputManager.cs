using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MegaKnight.GUI
{
    internal static class InputManager
    {
        static MouseState _currentMouse;
        static MouseState _prevMouse;

        static KeyboardState _currentKeys;
        static KeyboardState _prevKeys;
        public static void Update(GameTime gameTime)
        {
            _prevMouse = _currentMouse;
            _currentMouse = Mouse.GetState();
            _prevKeys = _currentKeys;
            _currentKeys = Keyboard.GetState();
        }
        public static bool GetMouseDown()
        {
            return _currentMouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
        }
        public static bool GetMousePressed()
        {
            return _currentMouse.LeftButton == ButtonState.Pressed;
        }
        public static bool GetMouseUp()
        {
            return _currentMouse.LeftButton == ButtonState.Released && _prevMouse.LeftButton == ButtonState.Pressed;
        }
        public static bool GetKeyDown(Keys key)
        {
            return _currentKeys.IsKeyDown(key) && !_prevKeys.IsKeyDown(key);
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
