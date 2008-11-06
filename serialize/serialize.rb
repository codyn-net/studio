require 'rexml/document'
require 'rexml/element'

module Cpg; end

module Cpg::Serialize
	def self.from_xml_name(name)
		name.gsub(/(-|^)([a-z])/) { $2.upcase }
	end

	def self.from_xml(element)
		klass = from_xml_name(element.name)
		c = nil
	
		begin
			s = [Cpg::Serialize, Cpg::Components, Cpg, Object].find do |space|
				next unless space.const_defined?(klass)

				c = space.const_get(klass)
				c && c.included_modules.include?(Cpg::Serialize::Object)
			end

		
			c = s.const_get(klass) if s
		rescue
			puts "Error in from_xml: #{$!}\n\t#{$@.join("\n\t")}"
			return nil
		end
	
		return nil if c == nil
	
		obj = c.new
	
		@loaded_objects ||= [{}]
	
		obj.from_xml(element, @loaded_objects)

		# handle references
		if obj.property_set?('id')
			@loaded_objects.first[obj.get_property('id')] = obj
		end
	
		obj
	end

	def self.reset
		@loaded_objects = [{}]
	end
end

# ex:ts=4:noet:
