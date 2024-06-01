import "../models/request/createPlayerRequest.model.tsp";
import "../models/player.model.tsp";
import "@typespec/http";

using TypeSpec.Http;

namespace Players {
    
    @route("/players")
    interface Players {
        @get listPlayers(@query tourneyId: int32): Player[];
        
        @get getPlayer(@path id: int32): Player;

        @post addPlayer(@body player: Player): Player;

        @put updatePlayer(@path id: int32, @body player: Player): Player;
        
        @delete deletePlayer(@path id: int32): void;
    }
}