package com.assambra.app.service;

import com.assambra.app.constant.GameConstant;
import com.assambra.app.model.CharacterInfoListModel;
import com.assambra.app.model.CharacterInfoModel;
import com.assambra.app.request.CreateCharacterRequest;
import com.assambra.common.entity.Character;
import com.assambra.common.entity.CharacterLocation;
import com.assambra.common.entity.User;
import com.assambra.common.repo.CharacterLocationRepo;
import com.assambra.common.repo.CharacterRepo;
import com.assambra.common.repo.UserRepo;
import com.tvd12.ezyfox.bean.annotation.EzySingleton;
import com.tvd12.ezyfox.util.EzyLoggable;
import com.tvd12.ezyfoxserver.entity.EzyUser;
import lombok.AllArgsConstructor;
import lombok.Setter;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

@Setter
@AllArgsConstructor
@EzySingleton("characterService")
public class CharacterService extends EzyLoggable {

    private final MaxIdService maxIdService;
    private final UserRepo userRepo;
    private final CharacterRepo characterRepo;
    private final CharacterLocationRepo characterLocationRepo;

    public Character getCharacter(Long id)
    {
        return characterRepo.findById(id);
    }

    public void createCharacter(EzyUser ezyuser, CreateCharacterRequest request)
    {
        User user = userRepo.findByField("username", ezyuser.getName());

        Character character = new Character();
        character.setId(maxIdService.incrementAndGet("character"));
        character.setUserId(user.getId());
        character.setName(request.getName());
        character.setUsername(user.getUsername());
        characterRepo.save(character);

        CharacterLocation characterLocation = new CharacterLocation();
        characterLocation.setId(maxIdService.incrementAndGet("characterLocation"));
        characterLocation.setCharacterId(character.getId());
        characterLocation.setRoom(GameConstant.START_ROOM);
        characterLocation.setPosition(GameConstant.START_POSITION);
        characterLocation.setRotation(GameConstant.START_ROTATION);
        characterLocationRepo.save(characterLocation);
    }

    public Boolean characterExist(String name)
    {
        return characterRepo.findByField("name", name) != null;
    }

    public List<Character> getAllCharactersOfUser (EzyUser ezyUser)
    {
        User user = userRepo.findByField("username", ezyUser.getName());
        return characterRepo.findListByField("userId", user.getId());
    }

    public List<CharacterLocation> getAllCharacterLocationsOfUser(EzyUser ezyUser)
    {
        User user = userRepo.findByField("username", ezyUser.getName());
        List<Character> characters = characterRepo.findListByField("userId", user.getId());

        if (characters.isEmpty()) {
            return new ArrayList<>();
        }

        List<Long> characterIds = characters.stream()
                .map(Character::getId)
                .collect(Collectors.toList());

        return characterLocationRepo.findByCharacterIds(characterIds);
    }

    public CharacterInfoListModel getCharacterInfoListModel(EzyUser ezyUser)
    {
        List<Character> allCharacters = getAllCharactersOfUser(ezyUser);
        List<CharacterLocation> allCharacterLocations = getAllCharacterLocationsOfUser(ezyUser);

        List<CharacterInfoModel> characterInfoModel = getListCharacterInfoModel(allCharacters, allCharacterLocations);

        return CharacterInfoListModel.builder()
                .characters(characterInfoModel)
                .build();
    }

    public List<CharacterInfoModel> getListCharacterInfoModel(List<Character> characters, List<CharacterLocation> characterLocations)
    {
        Map<Long, String> roomMap = characterLocations.stream()
                .collect(Collectors.toMap(CharacterLocation::getCharacterId, CharacterLocation::getRoom));

        List<CharacterInfoModel> answer = characters.stream().map(
                character -> {
                    String room = roomMap.get(character.getId());

                    return CharacterInfoModel.builder()
                        .id(character.getId())
                        .name(character.getName())
                        .room(room)
                        .build();
                }
        ).collect(Collectors.toList());

        return answer;
    }

    public CharacterLocation getCharacterLocation(Long characterId)
    {
        return characterLocationRepo.findByField("characterId", characterId);
    }
}
