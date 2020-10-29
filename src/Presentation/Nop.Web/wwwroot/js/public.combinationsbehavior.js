function createCombinationsBehavior(settings) {
  var defaultSettings = {
    contentEl: false,
    fetchUrl: false
  };

  return {
    settings: $.extend({}, defaultSettings, settings),
    params: {
      availableCombinations: [],
      availableAttributeIds: []
    },

    init: function () {
      var $contentEl = $(this.settings.contentEl);
      var $attributes = $('[data-attr]', $contentEl);
      if ($attributes && $attributes.length > 0) {
        var self = this;

        $.each($attributes, function (index, attribute) {
          var id = parseInt($(attribute).data('attr'));
          self.params.availableAttributeIds.push(id);
        });

        $attributes.on('change', function () {
          self.processCombinations();
        });
      }

      this.loadCombinations();
    },

    loadCombinations: function () {
      var self = this;

      $.ajax({
        cache: false,
        url: self.settings.fetchUrl,
        type: 'GET',
        success: function (response) {
          self.params.availableCombinations = response;

          var selectedAttributes = self.getSelectedAttributes();
          if (selectedAttributes && selectedAttributes.length > 0) {
            self.processCombinations();
          }
        },
        error: function () {
          self.params.availableCombinations = [];
        }
      });
    },

    processCombinations: function () {
      var availableAttributeIds = this.params.availableAttributeIds;
      if (!availableAttributeIds || availableAttributeIds.length === 0)
        return;

      var self = this;

      // disable all attribute values if combinations are empty
      var availableCombinations = this.params.availableCombinations;
      if (!availableCombinations || availableCombinations.length === 0) {
        var valueIds = this.getAttributeValueIds();
        $.each(valueIds, function (i, valueId) {
          self.toggleAttributeValue(valueId, false);
        });

        return;
      }

      var selectedAttributes = self.getSelectedAttributes();

      $.each(availableAttributeIds, function (i, attributeId) {
        var attibuteValueIds = self.getAttributeValueIds(attributeId);
        $.each(attibuteValueIds, function (i, valueId) {
          // if current attribute is already selected, then replace it
          var availableAttributes = $.grep(selectedAttributes, function (attribute) {
            return attribute.id !== attributeId && attribute.values.length > 0;
          });
          availableAttributes.push({ id: attributeId, values: [valueId] });

          // check if available attributes exists in combinations
          // otherwise just disable the value
          var combinations = self.getCombinationsByAttributeId(attributeId, availableAttributes);
          if (combinations && combinations.length > 0) {
            // check if any combination have stock with specified values
            // otherwise just disable the value 
            var existedCombinations = $.grep(combinations, function (combination) {
              var valueIdsByCombinations = $.map(combination.Attributes, function (attribute) {
                return attribute.ValueIds;
              });
              var existedAttributes = $.grep(availableAttributes, function (attribute) {
                return $.grep(attribute.values, function (selectedValueId) {
                  return $.inArray(selectedValueId, valueIdsByCombinations) !== -1;
                })
              });

              return combination.InStock && existedAttributes && existedAttributes.length === availableAttributes.length;
            });
            self.toggleAttributeValue(valueId, existedCombinations && existedCombinations.length > 0);
          } else {
            self.toggleAttributeValue(valueId, false);
          }
        });
      });

      $(this).trigger({ type: "processed" });
    },

    getCombinationsByAttributeId: function (attributeId, processedAttributes) {
      var availableCombinations = this.params.availableCombinations;
      if (!availableCombinations || availableCombinations.length === 0) {
        return;
      };

      return $.grep(availableCombinations, function (combination) {
        var found = $.grep(combination.Attributes, function (attribute) {
          return attribute.Id === attributeId;
        })[0];

        if (processedAttributes && processedAttributes.length > 0) {
          $.each(processedAttributes, function (i, processedAttribute) {
            found = found && $.grep(combination.Attributes, function (attribute) {
              var attrbiuteIsFound = attribute.Id === processedAttribute.id;

              // exclude unselected attribute values
              if (processedAttribute.values.length > 0) {
                $.each(processedAttribute.values, function (i, id) {
                  attrbiuteIsFound = attrbiuteIsFound && $.inArray(id, attribute.ValueIds) !== -1
                });
              }

              return attrbiuteIsFound;
            })[0];
          });
        }

        return found;
      });
    },

    getAttributeValueIds: function (attributeId) {
      var $contentEl = $(this.settings.contentEl);
      var $scope = attributeId ? $('[data-attr=' + attributeId + ']', $contentEl) : $contentEl;
      var $valueItems = $('[data-attr-value]', $scope);
      if ($valueItems) {
        return $.map($valueItems, function (item) {
          return parseInt($(item).data('attr-value'));
        });
      }
    },

    toggleAttributeValue: function (valueId, enabled) {
      if (!valueId)
        return;

      var $contentEl = $(this.settings.contentEl);
      var $value = $('[data-attr-value=' + valueId + ']', $contentEl);
      if (enabled) {
        $value.prop('disabled', false);
        $('input', $value).prop('disabled', false);
        $value.removeClass('disabled');
      } else {
        $value.prop('disabled', true);
        $('input', $value).prop('disabled', true);
        $value.addClass('disabled');
      }
    },

    getSelectedAttributes: function () {
      var availableAttributeIds = this.params.availableAttributeIds;
      if (availableAttributeIds && availableAttributeIds.length > 0) {
        var $contentEl = $(this.settings.contentEl);

        return $.map(availableAttributeIds, function (attributeId) {
          var selectedValues = [];

          var $attribute = $('[data-attr=' + attributeId + ']', $contentEl);
          var $attributeValues = $('[data-attr-value]', $attribute);
          if ($attributeValues && $attributeValues.length > 0) {
            $.each($attributeValues, function (index, value) {
              var $value = $(value);
              if ($value.is(':selected') || $('input', $value).is(':checked')) {
                var id = parseInt($value.data('attr-value'));
                selectedValues.push(id);
              }
            });
          }

          return {
            id: attributeId,
            values: selectedValues
          }
        });
      }
    }
  }
}