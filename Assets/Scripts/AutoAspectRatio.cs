

// Refactor: Extract shared logic into a base class and have the two component classes inherit from it.

// The original simple behavior: base cache comes from the rect itself.
public class AutoAspectRatio : BaseAutoAspectRatio
{
    // No overrides required; uses BaseAutoAspectRatio behavior.
}

// FullScreen variant: cache original size from parent rect (or screen) and recompute on screen change.