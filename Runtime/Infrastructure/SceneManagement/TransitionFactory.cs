using System;
using _Project.Core.Factories;
using TransitionsPlus;
using Object = UnityEngine.Object;

namespace _Project.Infrastructure.SceneManagement
{
    public class TransitionFactory : ITransitionFactory
    {
        private readonly IObjectFactory _objectFactory;
        private readonly TransitionProfile _transitionProfile;

        public TransitionFactory(IObjectFactory objectFactory, TransitionProfile transitionProfile)
        {
            _objectFactory = objectFactory ?? throw new ArgumentNullException(nameof(objectFactory));
            _transitionProfile = transitionProfile ?? throw new ArgumentNullException(nameof(transitionProfile));
        }

        public void Create(bool invert, bool autoDestroy, Action onComplete = null)
        {
            TransitionProfile instance = _objectFactory.Create(_transitionProfile);
            instance.invert = invert;

            TransitionAnimator animator = TransitionAnimator.Start(instance, autoDestroy);
            animator.onTransitionEnd.AddListener(() => Object.Destroy(instance));

            if (onComplete != null)
            {
                animator.onTransitionEnd.AddListener(() => onComplete());
            }
        }
    }
}
