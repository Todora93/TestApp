

var mk = {};

mk.controllers = {};

mk.controllers.Base = function (options) {
    if (!options)
        return;

    this._callbacks = options.callbacks || {};
};

mk.controllers.Base.prototype.getOpponent = function (f) {
    return this._opponents[f.getName(name)];
};

mk.controllers.Basic = function (options) {
    mk.controllers.Base.call(this, options);
};

mk.controllers.Basic.prototype = new mk.controllers.Base();

mk.controllers.Basic.prototype._initialize = function () {
    this._player = 0;
    this._addHandlers();
};

mk.controllers.keys.p1 = {
    RIGHT: 74,
    LEFT: 71,
    UP: 89,
    DOWN: 72,
    BLOCK: 16,
    HP: 65,
    LP: 83,
    LK: 68,
    HK: 70
};

mk.controllers.keys.p2 = {
    RIGHT: 39,
    LEFT: 37,
    UP: 38,
    DOWN: 40,
    BLOCK: 17,
    HP: 80,
    LP: 219,
    LK: 221,
    HK: 220
};


mk.controllers.Multiplayer = function (options) {
    mk.controllers.Basic.call(this, options);
};

mk.controllers.Multiplayer.prototype = new mk.controllers.Basic();

mk.controllers.Multiplayer.prototype._initialize = function () {
    this._addHandlers();
};

mk.controllers.Multiplayer.prototype._addHandlers = function () {
    var pressed = {},
        self = this,
        f1 = this.fighters[0],
        f2 = this.fighters[1];

    document.addEventListener('keydown', function (e) {
        pressed[e.keyCode] = true;
        var move = self._getMove(pressed, mk.controllers.keys.p1, 0);
        self._moveFighter(f1, move);
        move = self._getMove(pressed, mk.controllers.keys.p2, 1);
        self._moveFighter(f2, move);
    }, false);

    document.addEventListener('keyup', function (e) {
        delete pressed[e.keyCode];
        var move = self._getMove(pressed, mk.controllers.keys.p1, 0);
        self._moveFighter(f1, move);
        move = self._getMove(pressed, mk.controllers.keys.p2, 1);
        self._moveFighter(f2, move);
    }, false);
};

mk.controllers.Multiplayer.prototype._moveFighter = function (fighter, move) {
    if (move) {
        fighter.setMove(move);
    }
};